using FluentValidation;
using StrideFlow.Application.Models.Auth;

namespace StrideFlow.Application.Validation.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MinimumLength(32);
    }
}
