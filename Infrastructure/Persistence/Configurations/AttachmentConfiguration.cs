using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");

        builder.Property(a => a.AttachmentType).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(300);
        builder.Property(a => a.FilePath).IsRequired().HasMaxLength(400);
        builder.Property(a => a.FileType).IsRequired().HasMaxLength(100);

        builder.HasIndex(a => new { a.RequestId, a.AttachmentType });

        builder.HasOne(a => a.Request)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
