namespace StrideFlow.Application.Models.Dashboard;

public sealed record LeaderboardResponse(
    string Period,
    string City,
    string RewardCurrency,
    IReadOnlyList<RewardTierResponse> RewardPreview,
    IReadOnlyList<LeaderboardEntryResponse> Entries);
