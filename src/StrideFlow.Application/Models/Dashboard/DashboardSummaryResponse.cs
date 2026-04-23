namespace StrideFlow.Application.Models.Dashboard;

public sealed record DashboardSummaryResponse(
    int TodaySteps,
    double TodayDistanceMeters,
    double TodayCaloriesBurned,
    int ActiveMinutesToday,
    int CurrentStreakDays,
    double GoalProgressPercent,
    int WeeklySteps,
    double WeeklyDistanceMeters);
