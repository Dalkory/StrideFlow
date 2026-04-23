using System.ComponentModel.DataAnnotations;

namespace StrideFlow.Application.Configuration;

public class TrackingOptions
{
    public const string SectionName = "Tracking";

    [Range(5, 200)]
    public double MaxAcceptedAccuracyMeters { get; init; } = 65d;

    [Range(0, 50)]
    public double MinMeaningfulDistanceMeters { get; init; } = 2.5d;

    [Range(1, 20)]
    public double MaxReasonableSpeedMetersPerSecond { get; init; } = 7d;

    [Range(1, 250)]
    public int MaxPointsPerBatch { get; init; } = 100;
}
