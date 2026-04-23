using FluentValidation;
using StrideFlow.Application.Models.Sessions;

namespace StrideFlow.Application.Validation.Sessions;

public class TrackSessionPointsRequestValidator : AbstractValidator<TrackSessionPointsRequest>
{
    public TrackSessionPointsRequestValidator()
    {
        RuleFor(x => x.Points)
            .NotEmpty()
            .Must(points => points.Count <= 100)
            .WithMessage("A maximum of 100 points can be uploaded per request.");

        RuleForEach(x => x.Points)
            .ChildRules(point =>
            {
                point.RuleFor(x => x.Latitude).InclusiveBetween(-90d, 90d);
                point.RuleFor(x => x.Longitude).InclusiveBetween(-180d, 180d);
                point.RuleFor(x => x.AccuracyMeters).InclusiveBetween(0d, 500d);
                point.RuleFor(x => x.RecordedAt).NotEmpty();
            });
    }
}
