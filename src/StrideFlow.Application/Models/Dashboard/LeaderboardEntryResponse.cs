namespace StrideFlow.Application.Models.Dashboard;

public sealed record LeaderboardEntryResponse(
    Guid UserId,
    string DisplayName,
    string AccentColor,
    int Steps,
    double DistanceMeters,
    int Rank,
    bool IsCurrentUser);
