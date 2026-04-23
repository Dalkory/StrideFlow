using StrideFlow.Application.Configuration;

namespace StrideFlow.Api.Definitions;

public sealed class CorsDefinition : AppDefinition
{
    public override int OrderIndex => -500;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        var origins = builder.Configuration.GetSection(ClientAppOptions.SectionName)
            .Get<ClientAppOptions>()?.AllowedOrigins ?? ["http://localhost:5173"];

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (origins.Contains("*", StringComparer.Ordinal))
                {
                    policy.SetIsOriginAllowed(_ => true);
                }
                else
                {
                    policy.WithOrigins(origins);
                }

                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override Task ConfigureApplicationAsync(WebApplication app)
    {
        app.UseCors();
        return Task.CompletedTask;
    }
}
