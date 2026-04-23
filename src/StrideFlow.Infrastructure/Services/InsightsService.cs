using StrideFlow.Application.Abstractions.Engagement;
using StrideFlow.Application.Abstractions.Users;
using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Engagement;

namespace StrideFlow.Infrastructure.Services;

public sealed class InsightsService(
    IUserService userService,
    IRewardsService rewardsService,
    TimeProvider timeProvider) : IInsightsService
{
    public async Task<ActivityInsightsResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var dashboard = await userService.GetDashboardAsync(userId, cancellationToken).ConfigureAwait(false);
        var rewards = await rewardsService.GetSummaryAsync(userId, cancellationToken).ConfigureAwait(false);
        var coach = BuildCoach(dashboard, rewards);

        return new ActivityInsightsResponse(timeProvider.GetUtcNow(), coach, rewards);
    }

    private static ActivityCoachResponse BuildCoach(DashboardResponse dashboard, RewardSummaryResponse rewards)
    {
        var dailyGoal = dashboard.User.DailyStepGoal;
        var todaySteps = dashboard.Summary.TodaySteps;
        var remaining = Math.Max(0, dailyGoal - todaySteps);
        var weeklyAverage = dashboard.WeeklyTrend.Count > 0
            ? Math.Round(dashboard.WeeklyTrend.Average(x => x.Steps), 1, MidpointRounding.AwayFromZero)
            : 0d;
        var activeDays = dashboard.WeeklyTrend.Count(x => x.Steps > 0);
        var consistencyScore = Math.Round(activeDays / 7d * 100d, 1, MidpointRounding.AwayFromZero);
        var suggestedPerHour = Math.Max(0, (int)Math.Ceiling(remaining / 8d));

        return new ActivityCoachResponse(
            dailyGoal,
            remaining,
            suggestedPerHour,
            weeklyAverage,
            consistencyScore,
            BuildCoachMessage(remaining, rewards.Weekly),
            BuildAchievements(dashboard, rewards));
    }

    private static string BuildCoachMessage(int remainingSteps, RewardStandingResponse weeklyStanding)
    {
        if (remainingSteps == 0 && weeklyStanding.IsEligible)
        {
            return "Daily goal is complete and the Telegram Stars reward preview is active. Keep the rank until settlement.";
        }

        if (weeklyStanding.IsEligible)
        {
            return $"Reward preview is active at city rank #{weeklyStanding.Rank}. Finish {remainingSteps} more steps to close the daily goal.";
        }

        return $"Add {remainingSteps} more steps today to improve the city rank and unlock reward eligibility.";
    }

    private static IReadOnlyList<AchievementResponse> BuildAchievements(DashboardResponse dashboard, RewardSummaryResponse rewards)
    {
        return
        [
            BuildAchievement(
                "daily_goal",
                "Daily Goal",
                "Complete the personal daily step target.",
                "accent",
                dashboard.Summary.TodaySteps,
                dashboard.User.DailyStepGoal),
            BuildAchievement(
                "weekly_50k",
                "50K Week",
                "Reach 50,000 steps in the current weekly window.",
                "warm",
                dashboard.Summary.WeeklySteps,
                50_000),
            BuildAchievement(
                "first_route",
                "First Route",
                "Finish at least one walking session.",
                "accent",
                dashboard.RecentSessions.Count,
                1),
            BuildAchievement(
                "city_top_three",
                "City Podium",
                "Enter the top 3 of the weekly city leaderboard.",
                "warm",
                rewards.Weekly.Rank <= 3 && rewards.Weekly.Steps > 0 ? 3 : Math.Max(0, 4 - rewards.Weekly.Rank),
                3)
        ];
    }

    private static AchievementResponse BuildAchievement(
        string key,
        string title,
        string description,
        string tone,
        int currentValue,
        int targetValue)
    {
        var progress = targetValue > 0
            ? Math.Min(100d, Math.Round(currentValue / (double)targetValue * 100d, 1, MidpointRounding.AwayFromZero))
            : 0d;

        return new AchievementResponse(
            key,
            title,
            description,
            tone,
            progress >= 100d,
            progress,
            currentValue,
            targetValue);
    }
}
