using StrideFlow.Api.Hubs;

namespace StrideFlow.Api.Definitions;

public sealed class EndpointDefinition : AppDefinition
{
    public override int OrderIndex => 100;

    public override Task ConfigureApplicationAsync(WebApplication app)
    {
        app.MapControllers();
        app.MapHub<ActivityHub>("/hubs/activity").RequireAuthorization();
        app.MapGet("/health", () => Results.Ok(new HealthResponse("ok"))).AllowAnonymous();

        return Task.CompletedTask;
    }

    private sealed record HealthResponse(string Status);
}
