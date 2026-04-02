using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Database.DbContext;
using OPS.Database.Seed;

namespace OPS.IntegrationTests;

public class OrdersApiTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;
    private readonly WebAppFactory _factory;

    public OrdersApiTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DataSeeder.SeedAsync(db);

        var registerRequest = new RegisterRequest("Test User", $"test_{Guid.NewGuid()}@example.com", "Password1");
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new LoginRequest(registerRequest.Email, registerRequest.Password);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    private async Task<(HttpClient client, Guid customerId, Guid addressId)> SetupAuthenticatedClientAsync()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customers = await _client.GetFromJsonAsync<List<CustomerResponse>>("/api/v1/customers");
        var customer = customers!.Last();

        var addressRequest = new AddAddressRequest("123 Test St", null, "Test City", "TS", "12345", "US");
        var addressResponse = await _client.PostAsJsonAsync($"/api/v1/customers/{customer.Id}/addresses", addressRequest);
        addressResponse.EnsureSuccessStatusCode();
        var address = await addressResponse.Content.ReadFromJsonAsync<AddressResponse>();

        return (_client, customer.Id, address!.Id);
    }

    [Fact]
    public async Task POST_CreateOrder_Returns201WithOrder()
    {
        var (client, customerId, addressId) = await SetupAuthenticatedClientAsync();
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/v1/products");

        var request = new CreateOrderRequest(customerId, addressId,
            [new OrderItemRequest(products![0].Id, 2)]);

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order!.Status.Should().Be(Domain.Enums.OrderStatus.Pending);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(products[0].Price * 2);
    }

    [Fact]
    public async Task GET_OrderById_Returns200WithFullDetails()
    {
        var (client, customerId, addressId) = await SetupAuthenticatedClientAsync();
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/v1/products");

        var createRequest = new CreateOrderRequest(customerId, addressId,
            [new OrderItemRequest(products![0].Id, 1)]);
        var createResponse = await client.PostAsJsonAsync("/api/v1/orders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var response = await client.GetAsync($"/api/v1/orders/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GET_Orders_Returns200WithList()
    {
        var (client, _, _) = await SetupAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_CancelOrder_WhenPending_Returns200Cancelled()
    {
        var (client, customerId, addressId) = await SetupAuthenticatedClientAsync();
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/v1/products");

        var createRequest = new CreateOrderRequest(customerId, addressId,
            [new OrderItemRequest(products![0].Id, 1)]);
        var createResponse = await client.PostAsJsonAsync("/api/v1/orders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/v1/orders/{created!.Id}/cancel",
            new CancelOrderRequest("Changed mind"));

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await cancelResponse.Content.ReadFromJsonAsync<CancelOrderResponse>();
        result!.Status.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task PUT_UpdateStatus_InvalidTransition_Returns422()
    {
        var (client, customerId, addressId) = await SetupAuthenticatedClientAsync();
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/v1/products");

        var createRequest = new CreateOrderRequest(customerId, addressId,
            [new OrderItemRequest(products![0].Id, 1)]);
        var createResponse = await client.PostAsJsonAsync("/api/v1/orders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/orders/{created!.Id}/status",
            new UpdateOrderStatusRequest(Domain.Enums.OrderStatus.Delivered));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GET_OrderById_NotFound_Returns404()
    {
        var (client, _, _) = await SetupAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateOrder_WithoutAuth_Returns401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.PostAsJsonAsync("/api/v1/orders",
            new CreateOrderRequest(Guid.NewGuid(), Guid.NewGuid(), [new OrderItemRequest(Guid.NewGuid(), 1)]));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
