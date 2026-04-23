using StrideFlow.Application.Configuration;

namespace StrideFlow.Api.Definitions;

public sealed class RealtimeDefinition : AppDefinition
{
    public override int OrderIndex => -300;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        var signalR = builder.Services
            .AddSignalR()
            .AddJsonProtocol(options => WebDefinition.ConfigureJson(options.PayloadSerializerOptions));

        var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
        if (!string.IsNullOrWhiteSpace(redisOptions?.ConnectionString))
        {
            signalR.AddStackExchangeRedis(redisOptions.ConnectionString);
        }
    }
}
