using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrideFlow.Domain.Tracking;

namespace StrideFlow.Infrastructure.Database.Configurations;

public class WalkingSessionPointConfiguration : IEntityTypeConfiguration<WalkingSessionPoint>
{
    public void Configure(EntityTypeBuilder<WalkingSessionPoint> builder)
    {
        builder.ToTable("walking_session_points");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.SessionId, x.Sequence }).IsUnique();
        builder.HasIndex(x => new { x.SessionId, x.RecordedAt });

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Points)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
