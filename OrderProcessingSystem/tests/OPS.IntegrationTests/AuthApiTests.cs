using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Database.DbContext;
using OPS.Database.Seed;

namespace OPS.IntegrationTests;

public class AuthApiTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;
    private readonly WebAppFactory _factory;

    public AuthApiTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task EnsureDatabaseSeededAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DataSeeder.SeedAsync(db);
    }

    private static RegisterRequest NewValidRegisterRequest() =>
        new($"Test User {Guid.NewGuid()}", $"user_{Guid.NewGuid()}@example.com", "Password1");

    private async Task<RegisterRequest> RegisterUserAsync()
    {
        await EnsureDatabaseSeededAsync();
        var request = NewValidRegisterRequest();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.EnsureSuccessStatusCode();
        return request;
    }

    private async Task<string> GetValidTokenAsync()
    {
        var request = await RegisterUserAsync();
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(request.Email, request.Password));
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    // ── REGISTRATION ──────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Register_ValidRequest_Returns201WithCustomerData()
    {
        await EnsureDatabaseSeededAsync();
        var request = NewValidRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer!.Email.Should().Be(request.Email);
        customer.Name.Should().Be(request.Name);
        customer.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task POST_Register_ValidRequest_DoesNotReturnPasswordData()
    {
        await EnsureDatabaseSeededAsync();
        var request = NewValidRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContainAny("password", "passwordHash", "hash");
    }

    [Fact]
    public async Task POST_Register_DuplicateEmail_Returns400()
    {
        var registered = await RegisterUserAsync();
        var duplicate = registered with { Name = "Another User" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Register_DuplicateEmail_ErrorMentionsEmail()
    {
        var registered = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registered with { Name = "Other" });
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("already registered");
    }

    [Fact]
    public async Task POST_Register_ShortPassword_Returns400()
    {
        await EnsureDatabaseSeededAsync();
        var request = new RegisterRequest("John Doe", $"user_{Guid.NewGuid()}@example.com", "Abc1");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Register_PasswordMissingUppercase_Returns400()
    {
        await EnsureDatabaseSeededAsync();
        var request = new RegisterRequest("John Doe", $"user_{Guid.NewGuid()}@example.com", "password1");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Register_PasswordMissingDigit_Returns400()
    {
        await EnsureDatabaseSeededAsync();
        var request = new RegisterRequest("John Doe", $"user_{Guid.NewGuid()}@example.com", "PasswordNoDigit");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Register_InvalidEmailFormat_Returns400()
    {
        await EnsureDatabaseSeededAsync();
        var request = new RegisterRequest("John Doe", "not-a-valid-email", "Password1");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── LOGIN ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Login_ValidCredentials_Returns200WithToken()
    {
        var registered = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(registered.Email, registered.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.Token.Should().NotBeNullOrEmpty();
        auth.CustomerId.Should().NotBe(Guid.Empty);
        auth.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_Login_TokenExpiresIn24Hours()
    {
        var registered = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(registered.Email, registered.Password));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        auth!.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(24), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task POST_Login_TokenContainsExpectedClaims()
    {
        var registered = await RegisterUserAsync();
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(registered.Email, registered.Password));
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(auth!.Token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == registered.Email);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task POST_Login_WrongPassword_Returns400()
    {
        var registered = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(registered.Email, "WrongPassword999"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Login_UnknownEmail_Returns400()
    {
        await EnsureDatabaseSeededAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nobody@example.com", "Password1"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Login_WrongPassword_AndUnknownEmail_ReturnSameErrorMessage()
    {
        var registered = await RegisterUserAsync();

        var wrongPassResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(registered.Email, "WrongPassword999"));
        var unknownEmailResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nobody@example.com", "Password1"));

        var wrongPassBody = await wrongPassResponse.Content.ReadAsStringAsync();
        var unknownEmailBody = await unknownEmailResponse.Content.ReadAsStringAsync();

        wrongPassBody.Should().Contain("Invalid email or password");
        unknownEmailBody.Should().Contain("Invalid email or password");
    }

    // ── PROTECTED ENDPOINT ACCESS ─────────────────────────────────────────────

    [Fact]
    public async Task GET_Products_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Products_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this-is-not-a-valid-jwt");

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Products_WithTamperedToken_Returns401()
    {
        var token = await GetValidTokenAsync();
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Products_WithExpiredToken_Returns401()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("your-super-secret-jwt-key-minimum-32-characters-here"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiredToken = new JwtSecurityToken(
            issuer: "OPS.Web",
            audience: "OPS.Clients",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddHours(-25),
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(expiredToken);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Products_WithWrongIssuerToken_Returns401()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("your-super-secret-jwt-key-minimum-32-characters-here"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var wrongIssuerToken = new JwtSecurityToken(
            issuer: "WrongIssuer",
            audience: "OPS.Clients",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(wrongIssuerToken);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Products_WithWrongAudienceToken_Returns401()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("your-super-secret-jwt-key-minimum-32-characters-here"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var wrongAudienceToken = new JwtSecurityToken(
            issuer: "OPS.Web",
            audience: "WrongAudience",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(wrongAudienceToken);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Products_WithValidToken_Returns200()
    {
        var token = await GetValidTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Customers_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Customers_WithValidToken_Returns200()
    {
        var token = await GetValidTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Orders_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/orders",
            new CreateOrderRequest(Guid.NewGuid(), Guid.NewGuid(), [new OrderItemRequest(Guid.NewGuid(), 1)]));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
