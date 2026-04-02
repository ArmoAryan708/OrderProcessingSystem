using FluentAssertions;
using OPS.Application.DTOs.Requests;
using OPS.Application.Validators;

namespace OPS.UnitTests.Validators;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_PassesValidation()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidRequest_WithOptionalPhone_PassesValidation()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "Password1", "555-1234");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    // ── Name ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyName_FailsValidation()
    {
        var request = new RegisterRequest("", "john@example.com", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NameExactly200Chars_PassesValidation()
    {
        var request = new RegisterRequest(new string('A', 200), "john@example.com", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NameExceeds200Chars_FailsValidation()
    {
        var request = new RegisterRequest(new string('A', 201), "john@example.com", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Email ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyEmail_FailsValidation()
    {
        var request = new RegisterRequest("John Doe", "", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_InvalidEmailFormat_FailsValidation()
    {
        var request = new RegisterRequest("John Doe", "not-an-email", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EmailMissingAtSign_FailsValidation()
    {
        var request = new RegisterRequest("John Doe", "johnexample.com", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EmailMissingDomain_FailsValidation()
    {
        var request = new RegisterRequest("John Doe", "john@", "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EmailExceeds200Chars_FailsValidation()
    {
        var longEmail = new string('a', 190) + "@example.com";
        var request = new RegisterRequest("John Doe", longEmail, "Password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // ── Password ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyPassword_FailsValidation()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_PasswordTooShort_FailsWithCorrectMessage()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "Abc1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Password" &&
            e.ErrorMessage == "Password must be at least 8 characters.");
    }

    [Fact]
    public async Task Validate_PasswordExactly8Chars_WithAllRules_PassesValidation()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "Abcdef1!");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_PasswordMissingUppercase_FailsWithCorrectMessage()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "password1");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Password" &&
            e.ErrorMessage == "Password must contain at least one uppercase letter.");
    }

    [Fact]
    public async Task Validate_PasswordMissingDigit_FailsWithCorrectMessage()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "PasswordNoDigit");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Password" &&
            e.ErrorMessage == "Password must contain at least one number.");
    }

    [Fact]
    public async Task Validate_PasswordViolatesAllThreeRules_ReportsAllErrors()
    {
        var request = new RegisterRequest("John Doe", "john@example.com", "abc");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        var passwordErrors = result.Errors.Where(e => e.PropertyName == "Password").ToList();
        passwordErrors.Should().Contain(e => e.ErrorMessage == "Password must be at least 8 characters.");
        passwordErrors.Should().Contain(e => e.ErrorMessage == "Password must contain at least one uppercase letter.");
        passwordErrors.Should().Contain(e => e.ErrorMessage == "Password must contain at least one number.");
    }

    [Fact]
    public async Task Validate_MultipleFieldsInvalid_ReportsAllFieldErrors()
    {
        var request = new RegisterRequest("", "not-valid", "abc");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
