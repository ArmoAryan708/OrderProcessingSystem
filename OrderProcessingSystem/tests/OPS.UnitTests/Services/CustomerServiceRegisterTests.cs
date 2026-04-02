using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OPS.Application.DTOs.Requests;
using OPS.Application.Interfaces;
using OPS.Application.Interfaces.Repositories;
using OPS.Application.Services;
using OPS.Domain.Entities;
using ValidationException = OPS.Domain.Exceptions.ValidationException;

namespace OPS.UnitTests.Services;

public class CustomerServiceRegisterTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IValidator<RegisterRequest>> _validatorMock = new();
    private readonly CustomerService _sut;

    public CustomerServiceRegisterTests()
    {
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _sut = new CustomerService(_customerRepoMock.Object, _unitOfWorkMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsCustomerResponse()
    {
        _customerRepoMock.Setup(r => r.GetByEmailAsync("new@example.com", default))
            .ReturnsAsync((Customer?)null);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        var request = new RegisterRequest("New User", "new@example.com", "Password1");
        var result = await _sut.RegisterAsync(request);

        result.Name.Should().Be("New User");
        result.Email.Should().Be("new@example.com");
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsValidationException()
    {
        var existing = new Customer { Id = Guid.NewGuid(), Name = "Existing", Email = "taken@example.com", PasswordHash = "h" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("taken@example.com", default))
            .ReturnsAsync(existing);

        var request = new RegisterRequest("New User", "taken@example.com", "Password1");

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.RegisterAsync(request));
        ex.Errors.Should().ContainKey("Email");
        ex.Errors["Email"].Should().Contain(e => e.Contains("already registered"));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ErrorMessageContainsEmail()
    {
        var email = "taken@example.com";
        var existing = new Customer { Id = Guid.NewGuid(), Name = "Existing", Email = email, PasswordHash = "h" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync(email, default)).ReturnsAsync(existing);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.RegisterAsync(new RegisterRequest("User", email, "Password1")));

        ex.Errors["Email"].Should().Contain(e => e.Contains(email));
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsStoredHashed_NotPlainText()
    {
        Customer? savedCustomer = null;
        _customerRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), default))
            .Callback<Customer, CancellationToken>((c, _) => savedCustomer = c);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        await _sut.RegisterAsync(new RegisterRequest("User", "user@example.com", "Password1"));

        Assert.NotNull(savedCustomer);
        savedCustomer.PasswordHash.Should().NotBe("Password1");
        BCrypt.Net.BCrypt.Verify("Password1", savedCustomer.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_CommitsDatabaseOnce()
    {
        _customerRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Customer?)null);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        await _sut.RegisterAsync(new RegisterRequest("User", "user@example.com", "Password1"));

        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_DoesNotCommit()
    {
        var existing = new Customer { Id = Guid.NewGuid(), Name = "Existing", Email = "taken@example.com", PasswordHash = "h" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("taken@example.com", default))
            .ReturnsAsync(existing);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.RegisterAsync(new RegisterRequest("New User", "taken@example.com", "Password1")));

        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ResponseContainsNoPasswordData()
    {
        _customerRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Customer?)null);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        var result = await _sut.RegisterAsync(new RegisterRequest("User", "user@example.com", "Password1"));

        var resultJson = System.Text.Json.JsonSerializer.Serialize(result);
        resultJson.Should().NotContain("Password1");
        resultJson.Should().NotContain("PasswordHash");
    }
}
