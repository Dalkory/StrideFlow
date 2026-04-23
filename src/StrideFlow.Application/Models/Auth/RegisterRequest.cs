namespace StrideFlow.Application.Models.Auth;

public sealed record RegisterRequest(
    string Email,
    string Username,
    string DisplayName,
    string Password,
    double HeightCm,
    double WeightKg,
    int DailyStepGoal,
    string City,
    string TimeZoneId);
