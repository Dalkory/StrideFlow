using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Sessions;

namespace StrideFlow.Application.Abstractions.Realtime;

public interface IRealtimeNotifier
{
    Task BroadcastSessionUpdatedAsync(WalkingSessionDetailResponse session, CancellationToken cancellationToken);

    Task BroadcastSessionCompletedAsync(WalkingSessionDetailResponse session, CancellationToken cancellationToken);

    Task BroadcastLiveMapUpdatedAsync(IReadOnlyList<LiveSessionResponse> liveSessions, CancellationToken cancellationToken);

    Task BroadcastLeaderboardUpdatedAsync(IReadOnlyList<LeaderboardEntryResponse> leaderboard, CancellationToken cancellationToken);
}
