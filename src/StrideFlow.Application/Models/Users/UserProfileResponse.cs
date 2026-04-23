namespace StrideFlow.Application.Models.Users;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    string Bio,
    string City,
    string TimeZoneId,
    string AccentColor,
    double HeightCm,
    double WeightKg,
    double StepLengthMeters,
    int DailyStepGoal,
    bool IsProfilePublic,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt);
