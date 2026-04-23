using FluentValidation;
using StrideFlow.Application.Models.Auth;

namespace StrideFlow.Application.Validation.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(120);

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(24)
            .Matches("^[a-zA-Z0-9_]+$");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(60);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(120, 230);

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(35, 300);

        RuleFor(x => x.DailyStepGoal)
            .InclusiveBetween(1000, 50000);

        RuleFor(x => x.City)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(120);

        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .MaximumLength(120);
    }
}
