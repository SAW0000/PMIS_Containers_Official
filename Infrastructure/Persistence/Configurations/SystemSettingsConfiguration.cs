using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.Property(s => s.Id).HasDefaultValueSql("NEWID()");

        builder.Property(s => s.AllowedAttachmentExtensions)
            .IsRequired().HasMaxLength(100).IsUnicode(false);

        builder.Property(s => s.AllowedProfilePhotoExtensions)
            .IsRequired().HasMaxLength(100).IsUnicode(false);

        builder.Property(s => s.TermsAndConditionsText)
            .IsRequired().IsUnicode(true).HasColumnType("nvarchar(max)");

        builder.Property(s => s.TermsAgreementLabel)
            .IsRequired().HasMaxLength(1000).IsUnicode(true);

        builder.Property(s => s.OtpEmailMessageTemplate)
            .IsRequired().HasMaxLength(500).IsUnicode(true);

        builder.Property(s => s.OtpSmsMessageTemplate)
            .IsRequired().HasMaxLength(160).IsUnicode(true);

        builder.HasData(
            new SystemSettings
            {
                Id = SingletonId,
                LicenseValidityMonths = 12,
                ResponseSlaBusinessDays = 3,
                MaxActiveRequestsPerUser = 5,
                MaxUploadSizeMb = 20,
                AllowedAttachmentExtensions = "PDF,JPG,PNG",
                AllowedProfilePhotoExtensions = "JPG,PNG",
                MaxLoginAttempts = 10,
                LoginLockoutMinutes = 2,
                OtpExpiryMinutes = 5,
                OtpResendCooldownSeconds = 45,
                OtpMaxResendCount = 5,
                OtpEmailMessageTemplate = SystemSettings.DefaultOtpEmailMessageTemplate,
                OtpSmsMessageTemplate = SystemSettings.DefaultOtpSmsMessageTemplate,
                TermsAndConditionsText = SystemSettings.DefaultTermsAndConditionsText,
                TermsAgreementLabel = SystemSettings.DefaultTermsAgreementLabel,
                AuditLogRetentionDays = 0,
                NotificationRetentionDays = 0,
                AuditLogMaxCount = 0,
                NotificationMaxCount = 0,
                IsActive = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
