namespace StrideFlow.Application.Models.Sessions;

public sealed record LiveSessionResponse(
    Guid SessionId,
    Guid UserId,
    string DisplayName,
    string AccentColor,
    string SessionName,
    string Status,
    double Latitude,
    double Longitude,
    double TotalDistanceMeters,
    int TotalSteps,
    double CaloriesBurned,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WalkingSessionPointResponse> TailPoints);
