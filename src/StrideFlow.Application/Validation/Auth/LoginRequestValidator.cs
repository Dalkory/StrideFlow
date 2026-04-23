using FluentValidation;
using StrideFlow.Application.Models.Auth;

namespace StrideFlow.Application.Validation.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.DeviceName)
            .MaximumLength(120);
    }
}
