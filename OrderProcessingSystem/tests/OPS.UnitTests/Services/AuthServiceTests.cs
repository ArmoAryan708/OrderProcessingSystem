using FluentAssertions;
using Moq;
using OPS.Application.DTOs.Requests;
using OPS.Application.Interfaces.Repositories;
using OPS.Application.Interfaces.Services;
using OPS.Application.Services;
using OPS.Domain.Entities;
using OPS.Domain.Exceptions;

namespace OPS.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_customerRepoMock.Object, _jwtTokenServiceMock.Object);
    }

    private static Customer BuildCustomer(string email = "user@example.com", string password = "Password1") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        var customer = BuildCustomer();
        var token = "jwt-token";
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        _customerRepoMock.Setup(r => r.GetByEmailAsync(customer.Email, default))
            .ReturnsAsync(customer);
        _jwtTokenServiceMock.Setup(s => s.GenerateToken(customer))
            .Returns((token, expiresAt));

        var result = await _sut.LoginAsync(new LoginRequest(customer.Email, "Password1"));

        result.Token.Should().Be(token);
        result.ExpiresAt.Should().Be(expiresAt);
        result.CustomerId.Should().Be(customer.Id);
        result.Name.Should().Be(customer.Name);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsValidationException()
    {
        _customerRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Customer?)null);

        await _sut.Invoking(s => s.LoginAsync(new LoginRequest("unknown@example.com", "Password1")))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsValidationException()
    {
        var customer = BuildCustomer();
        _customerRepoMock.Setup(r => r.GetByEmailAsync(customer.Email, default))
            .ReturnsAsync(customer);

        await _sut.Invoking(s => s.LoginAsync(new LoginRequest(customer.Email, "WrongPassword1")))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_CallsGenerateToken()
    {
        var customer = BuildCustomer();
        _customerRepoMock.Setup(r => r.GetByEmailAsync(customer.Email, default))
            .ReturnsAsync(customer);
        _jwtTokenServiceMock.Setup(s => s.GenerateToken(customer))
            .Returns(("token", DateTimeOffset.UtcNow.AddHours(24)));

        await _sut.LoginAsync(new LoginRequest(customer.Email, "Password1"));

        _jwtTokenServiceMock.Verify(s => s.GenerateToken(customer), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_DoesNotCallGenerateToken()
    {
        _customerRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest("nobody@example.com", "Password1")));

        _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_AndWrongPassword_ReturnSameErrorMessage()
    {
        var customer = BuildCustomer();
        _customerRepoMock.SetupSequence(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Customer?)null)
            .ReturnsAsync(customer);

        var unknownEmailEx = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest("unknown@example.com", "Password1")));

        var wrongPasswordEx = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest(customer.Email, "WrongPassword1")));

        unknownEmailEx.Errors["Credentials"]
            .Should().BeEquivalentTo(wrongPasswordEx.Errors["Credentials"],
                "because same message prevents email enumeration");
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ErrorKeyIsCredentials()
    {
        _customerRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Customer?)null);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest("nobody@example.com", "Password1")));

        ex.Errors.Should().ContainKey("Credentials");
        ex.Errors["Credentials"].Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ErrorKeyIsCredentials()
    {
        var customer = BuildCustomer();
        _customerRepoMock.Setup(r => r.GetByEmailAsync(customer.Email, default))
            .ReturnsAsync(customer);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest(customer.Email, "WrongPassword1")));

        ex.Errors.Should().ContainKey("Credentials");
        ex.Errors["Credentials"].Should().Contain("Invalid email or password.");
    }
}
