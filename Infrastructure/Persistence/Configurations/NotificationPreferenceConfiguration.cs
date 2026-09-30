using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.Property(p => p.Id).HasDefaultValueSql("NEWID()");

        builder.Property(p => p.PreferenceKey).IsRequired().HasMaxLength(64);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.PreferenceKey }).IsUnique();
    }
}
