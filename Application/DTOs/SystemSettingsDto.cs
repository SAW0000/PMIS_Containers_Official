namespace PMIS.Application.DTOs;

public class SystemSettingsDto
{
    public required int LicenseValidityMonths { get; set; }
    public required int ResponseSlaBusinessDays { get; set; }

    public required int MaxActiveRequestsPerUser { get; set; }

    public required int MaxUploadSizeMb { get; set; }

    public IReadOnlyList<string> AllowedAttachmentExtensions { get; set; } = [];

    public IReadOnlyList<string> AllowedProfilePhotoExtensions { get; set; } = [];

    public required int MaxLoginAttempts { get; set; }
    public required int LoginLockoutMinutes { get; set; }

    public required int OtpExpiryMinutes { get; set; }

    public required int OtpResendCooldownSeconds { get; set; }

    public required int OtpMaxResendCount { get; set; }

    public string OtpEmailMessageTemplate { get; set; } = string.Empty;

    public string OtpSmsMessageTemplate { get; set; } = string.Empty;

    public string TermsAndConditionsText { get; set; } = string.Empty;

    public string TermsAgreementLabel { get; set; } = string.Empty;

    public required int AuditLogRetentionDays { get; set; }

    public required int NotificationRetentionDays { get; set; }

    public required int AuditLogMaxCount { get; set; }

    public required int NotificationMaxCount { get; set; }
}
