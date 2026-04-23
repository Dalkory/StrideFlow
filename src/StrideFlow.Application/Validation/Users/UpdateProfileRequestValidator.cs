using FluentValidation;
using StrideFlow.Application.Models.Users;

namespace StrideFlow.Application.Validation.Users;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(60);

        RuleFor(x => x.Bio)
            .MaximumLength(280);

        RuleFor(x => x.City)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(120);

        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.AccentColor)
            .NotEmpty()
            .Matches("^#([0-9a-fA-F]{6})$");

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(120, 230);

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(35, 300);

        RuleFor(x => x.StepLengthMeters)
            .InclusiveBetween(0.3d, 1.5d)
            .When(x => x.StepLengthMeters.HasValue);

        RuleFor(x => x.DailyStepGoal)
            .InclusiveBetween(1000, 50000);
    }
}
