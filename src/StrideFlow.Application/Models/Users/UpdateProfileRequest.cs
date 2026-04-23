namespace StrideFlow.Application.Models.Users;

public sealed record UpdateProfileRequest(
    string DisplayName,
    string Bio,
    string City,
    string TimeZoneId,
    string AccentColor,
    double HeightCm,
    double WeightKg,
    double? StepLengthMeters,
    int DailyStepGoal,
    bool IsProfilePublic);
