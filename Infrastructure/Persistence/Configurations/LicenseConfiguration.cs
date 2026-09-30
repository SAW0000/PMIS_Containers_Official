using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.Property(l => l.Id).HasDefaultValueSql("NEWID()");

        builder.Property(l => l.Status).IsRequired().HasConversion<string>().HasMaxLength(15);
        builder.Property(l => l.LicenseKey).HasMaxLength(200);
        builder.Property(l => l.RowVersion).IsRowVersion();

        builder.HasIndex(l => l.RequestId).IsUnique();
        builder.HasIndex(l => new { l.Status, l.EndDate });
    }
}
