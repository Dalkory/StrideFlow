using StrideFlow.Application.Models.Dashboard;

namespace StrideFlow.Application.Models.Engagement;

public sealed record RewardSummaryResponse(
    string PayoutProvider,
    bool IsTestMode,
    string SettlementPolicy,
    RewardStandingResponse Weekly,
    RewardStandingResponse Monthly,
    IReadOnlyList<RewardTierResponse> WeeklyTiers,
    IReadOnlyList<RewardTierResponse> MonthlyTiers);
