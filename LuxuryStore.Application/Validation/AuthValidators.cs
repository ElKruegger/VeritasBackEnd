using FluentValidation;
using LuxuryStore.Application.DTOs;

namespace LuxuryStore.Application.Validation;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();

        // Length is what actually makes a password strong; the letter+digit
        // rule only blocks the most trivial choices ("password", "12345678")
        // without the frustration of mandatory symbols. The upper bound
        // caps how much work PBKDF2 does per request (a 1 MB "password"
        // would otherwise be hashed in full on every attempt).
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        // Deliberately looser than registration: only shape/size checks.
        // Rejecting a login because the password "doesn't meet the policy"
        // would leak the policy and break accounts created under an older one.
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(100);
    }
}
