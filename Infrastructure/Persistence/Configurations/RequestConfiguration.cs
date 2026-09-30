using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.Property(r => r.Id).HasDefaultValueSql("NEWID()");

        builder.Property(r => r.RequestNumber).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(35);
        builder.Property(r => r.UserTypeDisplay).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Decision).HasMaxLength(500);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.Property(r => r.ApplicantFirstName).HasMaxLength(100);
        builder.Property(r => r.ApplicantLastName).HasMaxLength(100);
        builder.Property(r => r.ApplicantEmail).HasMaxLength(256);
        builder.Property(r => r.ApplicantMobile).HasMaxLength(20);

        builder.HasIndex(r => r.RequestNumber).IsUnique();
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequestDate);
        builder.HasIndex(r => r.ReviewedById);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Requests)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Company)
            .WithMany(c => c.Requests)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Project)
            .WithMany(p => p.Requests)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.System)
            .WithMany(s => s.Requests)
            .HasForeignKey(r => r.SystemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.License)
            .WithOne(l => l.Request)
            .HasForeignKey<License>(l => l.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
