using Microsoft.EntityFrameworkCore;
using StrideFlow.Application.Abstractions.Tracking;
using StrideFlow.Application.Abstractions.Users;
using StrideFlow.Application.Common;
using StrideFlow.Application.Configuration;
using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Users;
using StrideFlow.Domain.Users;
using StrideFlow.Infrastructure.Database;

namespace StrideFlow.Infrastructure.Services;

public class UserService(
    StrideFlowDbContext dbContext,
    ILiveSessionStore liveSessionStore,
    Microsoft.Extensions.Options.IOptions<RewardRulesOptions> rewardRulesOptions,
    IAdService adService,
    TimeProvider timeProvider) : IUserService
{
    public async Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return user.ToResponse();
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var stepLength = request.StepLengthMeters ?? GetDefaultStepLength(request.HeightCm);

        user.UpdateProfile(
            request.DisplayName.Trim(),
            request.Bio.Trim(),
            request.City.Trim(),
            request.TimeZoneId.Trim(),
            request.AccentColor,
            request.HeightCm,
            request.WeightKg,
            stepLength,
            request.DailyStepGoal,
            request.IsProfilePublic,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.ToResponse();
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        var timeZone = ResolveTimeZone(user.TimeZoneId);
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        var todayStartUtc = ToUtc(localToday, timeZone);
        var rangeStartUtc = ToUtc(localToday.AddDays(-6), timeZone);

        var weeklySessions = await dbContext.WalkingSessions
            .Where(x => x.UserId == userId && x.StartedAt >= rangeStartUtc)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var currentSession = await dbContext.WalkingSessions
            .Include(x => x.Points)
            .Where(x => x.UserId == userId && x.Status != Domain.Tracking.WalkingSessionStatus.Completed)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var weeklyTrend = BuildWeeklyTrend(weeklySessions, timeZone, localToday);
        var todayTrend = weeklyTrend.Last();
        var weeklySteps = weeklyTrend.Sum(x => x.Steps);
        var weeklyDistance = weeklyTrend.Sum(x => x.DistanceMeters);
        var streak = CalculateStreak(weeklyTrend);

        var summary = new DashboardSummaryResponse(
            todayTrend.Steps,
            todayTrend.DistanceMeters,
            todayTrend.CaloriesBurned,
            (int)Math.Round(weeklySessions.Where(x => x.StartedAt >= todayStartUtc).Sum(x => x.DurationSeconds) / 60d, MidpointRounding.AwayFromZero),
            streak,
            user.DailyStepGoal > 0 ? Math.Min(100d, Math.Round((double)todayTrend.Steps / user.DailyStepGoal * 100d, 1, MidpointRounding.AwayFromZero)) : 0d,
            weeklySteps,
            weeklyDistance);

        var recentSessions = weeklySessions
            .OrderByDescending(x => x.StartedAt)
            .Take(5)
            .Select(x => x.ToResponse())
            .ToList();

        var leaderboard = await GetLeaderboardAsync(userId, "day", 5, user.City, cancellationToken).ConfigureAwait(false);
        var adSlots = await adService.GetSlotsAsync(cancellationToken).ConfigureAwait(false);
        var activeWalkers = (await liveSessionStore.GetAllAsync(cancellationToken).ConfigureAwait(false))
            .Select(x => x.ToResponse())
            .OrderByDescending(x => x.TotalSteps)
            .ToList();

        return new DashboardResponse(
            user.ToResponse(),
            summary,
            currentSession?.ToDetailResponse(currentSession.Points.ToList()),
            recentSessions,
            weeklyTrend,
            leaderboard,
            adSlots,
            activeWalkers);
    }

    public async Task<LeaderboardResponse> GetLeaderboardAsync(Guid userId, string period, int limit, string? city, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        var timeZone = ResolveTimeZone(user.TimeZoneId);
        var startUtc = GetPeriodStartUtc(period, now, timeZone);
        var requestedCity = string.IsNullOrWhiteSpace(city) ? user.City : city.Trim();
        var isGlobal = requestedCity.Equals("all", StringComparison.OrdinalIgnoreCase);

        var aggregates = await dbContext.WalkingSessions
            .Where(x => x.StartedAt >= startUtc)
            .Join(
                dbContext.Users,
                session => session.UserId,
                currentUser => currentUser.Id,
                (session, currentUser) => new
                {
                    currentUser.Id,
                    currentUser.DisplayName,
                    currentUser.AccentColor,
                    currentUser.City,
                    session.TotalSteps,
                    session.TotalDistanceMeters
                })
            .Where(x => isGlobal || x.City == requestedCity)
            .GroupBy(x => new { x.Id, x.DisplayName, x.AccentColor })
            .Select(group => new
            {
                group.Key.Id,
                group.Key.DisplayName,
                group.Key.AccentColor,
                Steps = group.Sum(x => x.TotalSteps),
                DistanceMeters = group.Sum(x => x.TotalDistanceMeters)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rewardPreview = GetRewardPreview(period);

        if (aggregates.Count == 0)
        {
            return new LeaderboardResponse(
                NormalizePeriod(period),
                isGlobal ? "All cities" : requestedCity,
                "telegram_stars",
                rewardPreview,
                [new LeaderboardEntryResponse(user.Id, user.DisplayName, user.AccentColor, 0, 0d, 1, true)]);
        }

        var ranked = aggregates
            .OrderByDescending(x => x.Steps)
            .ThenByDescending(x => x.DistanceMeters)
            .Select((entry, index) => new LeaderboardEntryResponse(
                entry.Id,
                entry.DisplayName,
                entry.AccentColor,
                entry.Steps,
                Math.Round(entry.DistanceMeters, 2, MidpointRounding.AwayFromZero),
                index + 1,
                entry.Id == userId))
            .ToList();

        var result = ranked.Take(limit).ToList();
        if (result.All(x => x.UserId != userId))
        {
            var currentUserEntry = ranked.FirstOrDefault(x => x.UserId == userId);
            if (currentUserEntry is not null)
            {
                result.Add(currentUserEntry);
            }
        }

        return new LeaderboardResponse(
            NormalizePeriod(period),
            isGlobal ? "All cities" : requestedCity,
            "telegram_stars",
            rewardPreview,
            result);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new AppException(404, "User not found.", "user_not_found");
        }

        return user;
    }

    private static List<DailyTrendResponse> BuildWeeklyTrend(IEnumerable<Domain.Tracking.WalkingSession> sessions, TimeZoneInfo timeZone, DateOnly localToday)
    {
        var list = new List<DailyTrendResponse>(7);

        for (var day = 6; day >= 0; day--)
        {
            var currentDate = localToday.AddDays(-day);
            var daySessions = sessions
                .Where(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.StartedAt, timeZone).DateTime) == currentDate)
                .ToList();

            list.Add(new DailyTrendResponse(
                currentDate,
                daySessions.Sum(x => x.TotalSteps),
                Math.Round(daySessions.Sum(x => x.TotalDistanceMeters), 2, MidpointRounding.AwayFromZero),
                Math.Round(daySessions.Sum(x => x.CaloriesBurned), 2, MidpointRounding.AwayFromZero)));
        }

        return list;
    }

    private static int CalculateStreak(IReadOnlyList<DailyTrendResponse> trend)
    {
        var streak = 0;

        for (var i = trend.Count - 1; i >= 0; i--)
        {
            if (trend[i].Steps <= 0)
            {
                break;
            }

            streak++;
        }

        return streak;
    }

    private static DateTimeOffset GetPeriodStartUtc(string period, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);

        return NormalizePeriod(period) switch
        {
            "day" => ToUtc(localDate, timeZone),
            "week" => ToUtc(localDate.AddDays(-6), timeZone),
            "month" => ToUtc(new DateOnly(localDate.Year, localDate.Month, 1), timeZone),
            _ => throw new AppException(400, "Leaderboard period must be one of: day, week, month.", "invalid_period")
        };
    }

    private static DateTimeOffset ToUtc(DateOnly localDate, TimeZoneInfo timeZone)
    {
        var localDateTime = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone));
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static double GetDefaultStepLength(double heightCm)
    {
        return Math.Round((heightCm * 0.415d) / 100d, 2, MidpointRounding.AwayFromZero);
    }

    private static string NormalizePeriod(string period) => period.Trim().ToLowerInvariant();

    private IReadOnlyList<RewardTierResponse> GetRewardPreview(string period)
    {
        var source = NormalizePeriod(period) switch
        {
            "month" => rewardRulesOptions.Value.Monthly,
            "week" => rewardRulesOptions.Value.Weekly,
            "day" => rewardRulesOptions.Value.Weekly,
            _ => rewardRulesOptions.Value.Weekly
        };

        return source
            .OrderBy(x => x.Rank)
            .Select(x => new RewardTierResponse(x.Rank, x.TelegramStars))
            .ToList();
    }
}
