using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace StrideFlow.IntegrationTests.Fixtures;

public sealed class StrideFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly Dictionary<string, string?> environmentVariables = new();

    private readonly PostgreSqlContainer postgreSqlContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("strideflow_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer redisContainer = new RedisBuilder("redis:7-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
    }

    public async Task InitializeAsync()
    {
        await postgreSqlContainer.StartAsync();
        await redisContainer.StartAsync();

        environmentVariables["ConnectionStrings__Postgres"] = postgreSqlContainer.GetConnectionString();
        environmentVariables["Redis__ConnectionString"] = $"{redisContainer.Hostname}:{redisContainer.GetMappedPublicPort(6379)}";
        environmentVariables["Redis__InstanceName"] = "strideflow-integration";
        environmentVariables["Jwt__Issuer"] = "StrideFlow.Tests";
        environmentVariables["Jwt__Audience"] = "StrideFlow.Tests.Client";
        environmentVariables["Jwt__Key"] = "strideflow-tests-signing-key-1234567890";
        environmentVariables["ClientApp__AllowedOrigins__0"] = "http://localhost:5173";

        foreach (var (key, value) in environmentVariables)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        _ = CreateClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        foreach (var key in environmentVariables.Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        await postgreSqlContainer.DisposeAsync();
        await redisContainer.DisposeAsync();
        Dispose();
    }
}
