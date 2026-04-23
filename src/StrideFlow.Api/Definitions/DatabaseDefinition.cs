using Microsoft.EntityFrameworkCore;
using StrideFlow.Infrastructure.Database;

namespace StrideFlow.Api.Definitions;

public sealed class DatabaseDefinition : AppDefinition
{
    public override int OrderIndex => -950;

    public override async Task ConfigureApplicationAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StrideFlowDbContext>();
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
    }
}
