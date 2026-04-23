using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace StrideFlow.Api.Definitions;

public sealed class RateLimiterDefinition : AppDefinition
{
    public const string AuthPolicy = "auth";
    public const string TrackingPolicy = "tracking";

    public override int OrderIndex => -200;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, cancellationToken) =>
            {
                var problem = new ProblemDetails
                {
                    Title = "Too many requests.",
                    Detail = "The request rate is too high. Please wait a little and try again.",
                    Status = StatusCodes.Status429TooManyRequests
                };

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
            };

            options.AddFixedWindowLimiter(AuthPolicy, settings =>
            {
                settings.PermitLimit = 10;
                settings.Window = TimeSpan.FromMinutes(1);
                settings.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(TrackingPolicy, settings =>
            {
                settings.PermitLimit = 40;
                settings.Window = TimeSpan.FromSeconds(10);
                settings.QueueLimit = 0;
            });
        });
    }

    public override Task ConfigureApplicationAsync(WebApplication app)
    {
        app.UseRateLimiter();
        return Task.CompletedTask;
    }
}
