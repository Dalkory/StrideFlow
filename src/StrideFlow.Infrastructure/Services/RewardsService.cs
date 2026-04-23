using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StrideFlow.Application.Abstractions.Engagement;
using StrideFlow.Application.Common;
using StrideFlow.Application.Configuration;
using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Engagement;
using StrideFlow.Domain.Users;
using StrideFlow.Infrastructure.Database;

namespace StrideFlow.Infrastructure.Services;

public sealed class RewardsService(
    StrideFlowDbContext dbContext,
    IOptions<RewardRulesOptions> rewardRulesOptions,
    TimeProvider timeProvider) : IRewardsService
{
    public async Task<RewardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var timeZone = ResolveTimeZone(user.TimeZoneId);
        var now = timeProvider.GetUtcNow();

        var weekly = await BuildStandingAsync(user, "week", GetWeekStartUtc(now, timeZone), now, rewardRulesOptions.Value.Weekly, cancellationToken)
            .ConfigureAwait(false);
        var monthly = await BuildStandingAsync(user, "month", GetMonthStartUtc(now, timeZone), now, rewardRulesOptions.Value.Monthly, cancellationToken)
            .ConfigureAwait(false);

        return new RewardSummaryResponse(
            "telegram_stars",
            true,
            "Preview only. Settlement is prepared for Telegram Stars payouts and is not paid automatically yet.",
            weekly,
            monthly,
            ToRewardTiers(rewardRulesOptions.Value.Weekly),
            ToRewardTiers(rewardRulesOptions.Value.Monthly));
    }

    private async Task<RewardStandingResponse> BuildStandingAsync(
        User user,
        string period,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        IReadOnlyList<RewardTierOptions> rewardTiers,
        CancellationToken cancellationToken)
    {
        var city = user.City;

        var aggregates = await dbContext.WalkingSessions
            .Where(session => session.StartedAt >= startsAt && session.StartedAt <= endsAt)
            .Join(
                dbContext.Users.Where(currentUser => currentUser.City == city),
                session => session.UserId,
                currentUser => currentUser.Id,
                (session, currentUser) => new
                {
                    currentUser.Id,
                    session.TotalSteps,
                    session.TotalDistanceMeters
                })
            .GroupBy(x => x.Id)
            .Select(group => new
            {
                UserId = group.Key,
                Steps = group.Sum(x => x.TotalSteps),
                DistanceMeters = group.Sum(x => x.TotalDistanceMeters)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (aggregates.All(x => x.UserId != user.Id))
        {
            aggregates.Add(new { UserId = user.Id, Steps = 0, DistanceMeters = 0d });
        }

        var ranked = aggregates
            .OrderByDescending(x => x.Steps)
            .ThenByDescending(x => x.DistanceMeters)
            .Select((entry, index) => new
            {
                entry.UserId,
                entry.Steps,
                entry.DistanceMeters,
                Rank = index + 1
            })
            .ToList();

        var standing = ranked.Single(x => x.UserId == user.Id);
        var telegramStars = rewardTiers.FirstOrDefault(x => x.Rank == standing.Rank)?.TelegramStars;
        var isEligible = standing.Steps > 0 && telegramStars > 0;

        return new RewardStandingResponse(
            period,
            city,
            startsAt,
            endsAt,
            standing.Rank,
            standing.Steps,
            Math.Round(standing.DistanceMeters, 2, MidpointRounding.AwayFromZero),
            isEligible ? telegramStars : null,
            isEligible,
            isEligible ? "test_mode_reward_preview" : "not_eligible_yet");
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            throw new AppException(404, "User not found.", "user_not_found");
        }

        return user;
    }

    private static IReadOnlyList<RewardTierResponse> ToRewardTiers(IEnumerable<RewardTierOptions> rewardTiers)
    {
        return rewardTiers
            .OrderBy(x => x.Rank)
            .Select(x => new RewardTierResponse(x.Rank, x.TelegramStars))
            .ToList();
    }

    private static DateTimeOffset GetWeekStartUtc(DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        return ToUtc(localToday.AddDays(-6), timeZone);
    }

    private static DateTimeOffset GetMonthStartUtc(DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        return ToUtc(new DateOnly(localToday.Year, localToday.Month, 1), timeZone);
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
}
