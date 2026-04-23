using Microsoft.AspNetCore.SignalR;
using StrideFlow.Application.Abstractions.Realtime;
using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Sessions;
using StrideFlow.Api.Hubs;

namespace StrideFlow.Api.Infrastructure;

public class SignalRRealtimeNotifier(IHubContext<ActivityHub> hubContext) : IRealtimeNotifier
{
    public Task BroadcastSessionUpdatedAsync(WalkingSessionDetailResponse session, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(ActivityHub.OverviewGroup)
            .SendAsync("sessionUpdated", session, cancellationToken);
    }

    public Task BroadcastSessionCompletedAsync(WalkingSessionDetailResponse session, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(ActivityHub.OverviewGroup)
            .SendAsync("sessionCompleted", session, cancellationToken);
    }

    public Task BroadcastLiveMapUpdatedAsync(IReadOnlyList<LiveSessionResponse> liveSessions, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(ActivityHub.OverviewGroup)
            .SendAsync("liveMapUpdated", liveSessions, cancellationToken);
    }

    public Task BroadcastLeaderboardUpdatedAsync(IReadOnlyList<LeaderboardEntryResponse> leaderboard, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(ActivityHub.OverviewGroup)
            .SendAsync("leaderboardUpdated", leaderboard, cancellationToken);
    }
}
