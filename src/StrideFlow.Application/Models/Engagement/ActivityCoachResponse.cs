namespace StrideFlow.Application.Models.Engagement;

public sealed record ActivityCoachResponse(
    int DailyGoal,
    int RemainingStepsToday,
    int SuggestedStepsPerHour,
    double WeeklyAverageSteps,
    double ConsistencyScore,
    string Message,
    IReadOnlyList<AchievementResponse> Achievements);
