using FluentValidation.TestHelper;
using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Validation;

namespace LuxuryStore.UnitTests.Application.Validation;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator = new();

    private static RegisterDto ValidDto() => new()
    {
        Email = "erick@example.com",
        Password = "Veritas2026",
        FirstName = "Erick",
        LastName = "Krug"
    };

    [Fact]
    public void Valid_registration_passes()
    {
        _validator.TestValidate(ValidDto()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("missing-at.example.com")]
    public void Invalid_email_is_rejected(string email)
    {
        var dto = ValidDto();
        dto.Email = email;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1")]       // under 8 characters
    [InlineData("12345678")]     // no letter
    [InlineData("onlyletters")]  // no digit
    public void Weak_password_is_rejected(string password)
    {
        var dto = ValidDto();
        dto.Password = password;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_longer_than_100_characters_is_rejected()
    {
        // Upper bound caps the PBKDF2 work an attacker can force per request.
        var dto = ValidDto();
        dto.Password = "a1" + new string('x', 99);

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Missing_names_are_rejected()
    {
        var dto = ValidDto();
        dto.FirstName = "";
        dto.LastName = "";

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }
}

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator = new();

    [Fact]
    public void Login_does_not_enforce_the_registration_password_policy()
    {
        // Accounts created under an older policy must still be able to log in,
        // and a login error must never reveal what the policy is.
        var dto = new LoginDto { Email = "erick@example.com", Password = "12345678" };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_credentials_are_rejected()
    {
        var result = _validator.TestValidate(new LoginDto());

        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
