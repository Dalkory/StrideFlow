using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StrideFlow.Api.Hubs;

[Authorize]
public class ActivityHub : Hub
{
    public const string OverviewGroup = "overview";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, OverviewGroup);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, OverviewGroup);
        await base.OnDisconnectedAsync(exception);
    }
}
