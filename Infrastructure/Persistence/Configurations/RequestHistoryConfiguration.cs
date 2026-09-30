using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class RequestHistoryConfiguration : IEntityTypeConfiguration<RequestHistory>
{
    public void Configure(EntityTypeBuilder<RequestHistory> builder)
    {
        builder.Property(h => h.Id).HasDefaultValueSql("NEWID()");

        builder.Property(h => h.EventLabel).IsRequired().HasMaxLength(100);

        builder.HasIndex(h => new { h.RequestId, h.EventDate });

        builder.HasOne(h => h.Request)
            .WithMany(r => r.RequestHistories)
            .HasForeignKey(h => h.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
