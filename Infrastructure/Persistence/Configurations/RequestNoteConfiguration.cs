using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class RequestNoteConfiguration : IEntityTypeConfiguration<RequestNote>
{
    public void Configure(EntityTypeBuilder<RequestNote> builder)
    {
        builder.Property(n => n.Id).HasDefaultValueSql("NEWID()");

        builder.Property(n => n.NoteText).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.NoteType).IsRequired().HasConversion<string>().HasMaxLength(15);

        builder.HasIndex(n => n.RequestId);

        builder.HasOne(n => n.Request)
            .WithMany(r => r.RequestNotes)
            .HasForeignKey(n => n.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Reason)
            .WithMany(o => o.RequestNotes)
            .HasForeignKey(n => n.ReasonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
