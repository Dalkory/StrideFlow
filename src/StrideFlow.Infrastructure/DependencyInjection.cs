using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Tracking;
using StrideFlow.Application.Abstractions.Users;
using StrideFlow.Application.Configuration;
using StrideFlow.Domain.Users;
using StrideFlow.Infrastructure.Authentication;
using StrideFlow.Infrastructure.Database;
using StrideFlow.Infrastructure.LiveTracking;
using StrideFlow.Infrastructure.Services;

namespace StrideFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStrideFlowInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<TrackingOptions>()
            .Bind(configuration.GetSection(TrackingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ClientAppOptions>()
            .Bind(configuration.GetSection(ClientAppOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<RewardRulesOptions>()
            .Bind(configuration.GetSection(RewardRulesOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<AdSlotsOptions>()
            .Bind(configuration.GetSection(AdSlotsOptions.SectionName))
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<StrideFlowDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITrackingService, TrackingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IAdService, AdService>();
        services.AddSingleton<IAuthTokenService, JwtTokenService>();
        services.AddSingleton<ILiveSessionStore, RedisLiveSessionStore>();

        return services;
    }
}
