using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrideFlow.Domain.Tracking;

namespace StrideFlow.Infrastructure.Database.Configurations;

public class WalkingSessionConfiguration : IEntityTypeConfiguration<WalkingSession>
{
    public void Configure(EntityTypeBuilder<WalkingSession> builder)
    {
        builder.ToTable("walking_sessions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.StartedAt);

        builder.HasOne(x => x.User)
            .WithMany(x => x.WalkingSessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(WalkingSession.Points))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
