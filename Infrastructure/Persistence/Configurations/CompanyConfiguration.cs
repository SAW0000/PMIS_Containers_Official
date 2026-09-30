using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(c => c.Id).HasDefaultValueSql("NEWID()");

        builder.Property(c => c.CompanyName).HasMaxLength(200);
        builder.Property(c => c.CommercialNo).HasMaxLength(50);
        builder.Property(c => c.CompanyActivity).HasMaxLength(300);
        builder.Property(c => c.VatNo).HasMaxLength(50);
        builder.Property(c => c.NationalAddress).HasMaxLength(500);
        builder.Property(c => c.Iban).HasMaxLength(34);
        builder.Property(c => c.LogoFileRef).HasMaxLength(400);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(256);

        builder.HasIndex(c => c.CommercialNo);
    }
}
