using FluentValidation;
using StrideFlow.Application.Models.Sessions;

namespace StrideFlow.Application.Validation.Sessions;

public class StartSessionRequestValidator : AbstractValidator<StartSessionRequest>
{
    public StartSessionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(80);
    }
}
