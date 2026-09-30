using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class TargetSystemConfiguration : IEntityTypeConfiguration<TargetSystem>
{
    public static readonly Guid UnifierSystemId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<TargetSystem> builder)
    {
        builder.Property(s => s.Id).HasDefaultValueSql("NEWID()");

        builder.Property(s => s.SystemName).IsRequired().HasMaxLength(200);

        builder.HasIndex(s => s.SystemName).IsUnique();

        builder.HasData(
            new TargetSystem { Id = UnifierSystemId, SystemName = "Oracle Primavera Unifier", IsActive = true, CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
