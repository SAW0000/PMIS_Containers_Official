using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public static readonly Guid RajhiAccountId = Guid.Parse("00000000-0000-0000-0000-000000000201");
    public static readonly Guid AhliAccountId = Guid.Parse("00000000-0000-0000-0000-000000000202");

    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");

        builder.Property(a => a.Iban).IsRequired().HasMaxLength(34);
        builder.Property(a => a.BankName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.AccountHolderName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.TransferAmount).IsRequired().HasMaxLength(30);
        builder.Property(a => a.PurposeDescription).IsRequired().HasMaxLength(200);
        builder.Property(a => a.SortOrder).IsRequired().HasDefaultValue(0);

        builder.HasIndex(a => new { a.SortOrder, a.CreatedDate });

        builder.HasIndex(a => a.Iban).IsUnique();

        builder.HasData(
            new BankAccount
            {
                Id = RajhiAccountId,
                Iban = "SA03 8000 0000 6080 1016 7519",
                BankName = "مصرف الراجحي",
                AccountHolderName = "أمانة منطقة المدينة المنورة",
                TransferAmount = "5000",
                PurposeDescription = string.Empty,
                SortOrder = 0,
                IsActive = true,
                CreatedDate = SeedDate
            },
            new BankAccount
            {
                Id = AhliAccountId,
                Iban = "SA44 2000 0001 2345 6789 1234",
                BankName = "البنك الأهلي السعودي",
                AccountHolderName = "أمانة منطقة المدينة المنورة",
                TransferAmount = "7500",
                PurposeDescription = string.Empty,
                SortOrder = 1,
                IsActive = true,
                CreatedDate = SeedDate
            }
        );
    }
}
