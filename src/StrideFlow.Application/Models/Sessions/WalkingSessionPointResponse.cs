namespace StrideFlow.Application.Models.Sessions;

public sealed record WalkingSessionPointResponse(
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    double DistanceFromPreviousMeters,
    int StepDelta,
    double SpeedMetersPerSecond,
    DateTimeOffset RecordedAt);
