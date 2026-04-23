namespace StrideFlow.Application.Models.Engagement;

public sealed record ActivityInsightsResponse(
    DateTimeOffset GeneratedAt,
    ActivityCoachResponse Coach,
    RewardSummaryResponse Rewards);
