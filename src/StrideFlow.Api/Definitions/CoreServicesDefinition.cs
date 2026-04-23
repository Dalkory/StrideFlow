using StrideFlow.Api.Infrastructure;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Realtime;
using StrideFlow.Infrastructure;

namespace StrideFlow.Api.Definitions;

public sealed class CoreServicesDefinition : AppDefinition
{
    public override int OrderIndex => -900;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

        builder.Services.AddStrideFlowInfrastructure(builder.Configuration);
    }
}
