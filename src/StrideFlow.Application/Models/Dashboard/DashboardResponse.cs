using StrideFlow.Application.Models.Ads;
using StrideFlow.Application.Models.Sessions;
using StrideFlow.Application.Models.Users;

namespace StrideFlow.Application.Models.Dashboard;

public sealed record DashboardResponse(
    UserProfileResponse User,
    DashboardSummaryResponse Summary,
    WalkingSessionDetailResponse? CurrentSession,
    IReadOnlyList<WalkingSessionResponse> RecentSessions,
    IReadOnlyList<DailyTrendResponse> WeeklyTrend,
    LeaderboardResponse Leaderboard,
    IReadOnlyList<AdSlotResponse> AdSlots,
    IReadOnlyList<LiveSessionResponse> ActiveWalkers);
