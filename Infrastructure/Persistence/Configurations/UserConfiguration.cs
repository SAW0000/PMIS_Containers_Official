using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Id).HasDefaultValueSql("NEWID()");

        builder.Ignore(u => u.PrimaryRole);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.Mobile).HasMaxLength(20);
        builder.Property(u => u.PhotoFileRef).HasMaxLength(400);
        builder.Property(u => u.NationalId).HasMaxLength(20);

        builder.Property(u => u.ArabicFirstName).HasMaxLength(200);
        builder.Property(u => u.ArabicFatherName).HasMaxLength(200);
        builder.Property(u => u.ArabicGrandFatherName).HasMaxLength(200);
        builder.Property(u => u.ArabicFamilyName).HasMaxLength(200);
        builder.Property(u => u.ArabicNationality).HasMaxLength(200);
        builder.Property(u => u.Gender)
            .HasConversion(
                gender => gender.HasValue ? GenderFormat.ToArabicLabel(gender.Value) : null,
                label => label == null ? (Gender?)null : GenderFormat.Parse(label))
            .HasMaxLength(10);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Users_Gender",
            "[Gender] IS NULL OR [Gender] IN (N'ذكر', N'أنثى')"));

        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        builder.HasIndex(u => u.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL");
    }
}
