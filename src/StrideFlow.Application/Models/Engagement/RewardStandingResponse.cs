namespace StrideFlow.Application.Models.Engagement;

public sealed record RewardStandingResponse(
    string Period,
    string City,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int Rank,
    int Steps,
    double DistanceMeters,
    int? TelegramStars,
    bool IsEligible,
    string Status);
