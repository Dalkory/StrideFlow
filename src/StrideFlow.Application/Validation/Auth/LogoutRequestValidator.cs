using FluentValidation;
using StrideFlow.Application.Models.Auth;

namespace StrideFlow.Application.Validation.Auth;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MinimumLength(32);
    }
}
