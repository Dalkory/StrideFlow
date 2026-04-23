namespace StrideFlow.Application.Models.Sessions;

public sealed record WalkingSessionResponse(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? LastPointRecordedAt,
    DateTimeOffset? CompletedAt,
    double TotalDistanceMeters,
    int TotalSteps,
    double CaloriesBurned,
    int DurationSeconds);
