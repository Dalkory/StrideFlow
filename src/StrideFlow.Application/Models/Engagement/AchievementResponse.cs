namespace StrideFlow.Application.Models.Engagement;

public sealed record AchievementResponse(
    string Key,
    string Title,
    string Description,
    string Tone,
    bool IsUnlocked,
    double ProgressPercent,
    int CurrentValue,
    int TargetValue);
