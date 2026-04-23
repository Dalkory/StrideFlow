using Microsoft.EntityFrameworkCore;
using StrideFlow.Domain.Auth;
using StrideFlow.Domain.Tracking;
using StrideFlow.Domain.Users;

namespace StrideFlow.Infrastructure.Database;

public class StrideFlowDbContext(DbContextOptions<StrideFlowDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<WalkingSession> WalkingSessions => Set<WalkingSession>();

    public DbSet<WalkingSessionPoint> WalkingSessionPoints => Set<WalkingSessionPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StrideFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
