using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrideFlow.Domain.Users;

namespace StrideFlow.Infrastructure.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(24).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(60).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Bio).HasMaxLength(280).IsRequired();
        builder.Property(x => x.City).HasMaxLength(120).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AccentColor).HasMaxLength(7).IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasIndex(x => x.City);

        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.WalkingSessions))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
