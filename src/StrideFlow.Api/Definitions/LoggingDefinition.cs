using Serilog;

namespace StrideFlow.Api.Definitions;

public sealed class LoggingDefinition : AppDefinition
{
    public override int OrderIndex => -1000;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, _, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.Console();
        });
    }

    public override Task ConfigureApplicationAsync(WebApplication app)
    {
        app.UseSerilogRequestLogging();
        return Task.CompletedTask;
    }
}
