namespace StrideFlow.Application.Models.Sessions;

public sealed record WalkingSessionDetailResponse(
    WalkingSessionResponse Session,
    IReadOnlyList<WalkingSessionPointResponse> Points,
    double AveragePaceSecondsPerKilometer,
    double AverageSpeedMetersPerSecond);
