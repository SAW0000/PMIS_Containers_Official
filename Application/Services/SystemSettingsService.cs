using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Application.Services;

public class SystemSettingsService(
    IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IReferenceDataCache cache) : ISystemSettingsService
{
    private const string CacheKey = "system-settings";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private const int MaxLicenseValidityMonths = 120;
    private const int MaxResponseSlaBusinessDays = 60;
    private const int MaxActiveRequestsPerUserCeiling = 100;
    private const int MaxUploadSizeMbCeiling = 200;
    private const int MaxLoginAttemptsCeiling = 20;
    private const int MaxLoginLockoutMinutesCeiling = 60;

    private const int MaxOtpExpiryMinutesCeiling = 60;

    private const int MaxOtpResendCooldownSecondsCeiling = 600;
    private const int MaxOtpResendCountCeiling = 20;

    private const int OtpEmailMessageTemplateMaxLength = 500;

    private const int OtpSmsMessageTemplateMaxLength = 70;

    private const int MaxRetentionDaysCeiling = 3650;

    private const int MaxRetentionCountCeiling = 10_000_000;

    private const int TermsAndConditionsTextMaxLength = 20_000;
    private const int TermsAgreementLabelMaxLength = 1000;

    private static readonly string[] SupportedAttachmentExtensions = ["PDF", "JPG", "JPEG", "PNG"];

    private static readonly string[] SupportedProfilePhotoExtensions = ["JPG", "JPEG", "PNG"];

    public async Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<SystemSettingsDto>(CacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var settings = await unitOfWork.SystemSettings.GetByIdAsync(SystemSettingsConfiguration.SingletonId, cancellationToken)
            ?? throw new InvalidOperationException("SystemSettings singleton row is missing — check the InitialCreate migration's seed data.");

        var dto = new SystemSettingsDto
        {
            LicenseValidityMonths = settings.LicenseValidityMonths,
            ResponseSlaBusinessDays = settings.ResponseSlaBusinessDays,
            MaxActiveRequestsPerUser = settings.MaxActiveRequestsPerUser,
            MaxUploadSizeMb = settings.MaxUploadSizeMb,
            AllowedAttachmentExtensions = SplitExtensions(settings.AllowedAttachmentExtensions),
            AllowedProfilePhotoExtensions = SplitExtensions(settings.AllowedProfilePhotoExtensions),
            MaxLoginAttempts = settings.MaxLoginAttempts,
            LoginLockoutMinutes = settings.LoginLockoutMinutes,
            OtpExpiryMinutes = settings.OtpExpiryMinutes,
            OtpResendCooldownSeconds = settings.OtpResendCooldownSeconds,
            OtpMaxResendCount = settings.OtpMaxResendCount,
            OtpEmailMessageTemplate = settings.OtpEmailMessageTemplate,
            OtpSmsMessageTemplate = settings.OtpSmsMessageTemplate,
            TermsAndConditionsText = settings.TermsAndConditionsText,
            TermsAgreementLabel = settings.TermsAgreementLabel,
            AuditLogRetentionDays = settings.AuditLogRetentionDays,
            NotificationRetentionDays = settings.NotificationRetentionDays,
            AuditLogMaxCount = settings.AuditLogMaxCount,
            NotificationMaxCount = settings.NotificationMaxCount
        };

        await cache.SetAsync(CacheKey, dto, CacheTtl);
        return dto;
    }

    public async Task<string?> UpdateAsync(
        SystemSettingsDto settings, RequestActor actor, CancellationToken cancellationToken = default)
    {
        if (ValidateRequestLimits(settings) is { } requestLimitsError)
        {
            return requestLimitsError;
        }

        var extensions = NormalizeExtensions(settings.AllowedAttachmentExtensions);
        if (ValidateExtensions(extensions, SupportedAttachmentExtensions, "يجب اختيار نوع ملف واحد على الأقل", "نوع ملف غير مدعوم") is { } extensionsError)
        {
            return extensionsError;
        }

        var photoExtensions = NormalizeExtensions(settings.AllowedProfilePhotoExtensions);
        if (ValidateExtensions(photoExtensions, SupportedProfilePhotoExtensions, "يجب اختيار امتداد واحد على الأقل للصورة الشخصية", "امتداد غير مدعوم للصورة الشخصية") is { } photoExtensionsError)
        {
            return photoExtensionsError;
        }

        if (ValidateLoginAndOtpSettings(settings) is { } loginOtpError)
        {
            return loginOtpError;
        }

        var emailTemplate = settings.OtpEmailMessageTemplate?.Trim() ?? string.Empty;
        if (ValidateOtpMessageTemplate(emailTemplate, OtpEmailMessageTemplateMaxLength, OtpMessageTemplate.EmailToken) is { } emailTemplateError)
        {
            return emailTemplateError;
        }

        var smsTemplate = settings.OtpSmsMessageTemplate?.Trim() ?? string.Empty;
        if (ValidateOtpMessageTemplate(smsTemplate, OtpSmsMessageTemplateMaxLength, OtpMessageTemplate.SmsToken) is { } smsTemplateError)
        {
            return smsTemplateError;
        }

        var termsText = settings.TermsAndConditionsText?.Trim() ?? string.Empty;
        if (ValidateRequiredText(termsText, TermsAndConditionsTextMaxLength, "نص الشروط والأحكام") is { } termsTextError)
        {
            return termsTextError;
        }

        var termsAgreementLabel = settings.TermsAgreementLabel?.Trim() ?? string.Empty;
        if (ValidateRequiredText(termsAgreementLabel, TermsAgreementLabelMaxLength, "نص إقرار الموافقة") is { } termsAgreementLabelError)
        {
            return termsAgreementLabelError;
        }

        if (ValidateRetentionSettings(settings) is { } retentionError)
        {
            return retentionError;
        }

        var entity = await unitOfWork.SystemSettings.GetByIdAsync(SystemSettingsConfiguration.SingletonId, cancellationToken)
            ?? throw new InvalidOperationException("SystemSettings singleton row is missing — check the InitialCreate migration's seed data.");

        entity.LicenseValidityMonths = settings.LicenseValidityMonths;
        entity.ResponseSlaBusinessDays = settings.ResponseSlaBusinessDays;
        entity.MaxActiveRequestsPerUser = settings.MaxActiveRequestsPerUser;
        entity.MaxUploadSizeMb = settings.MaxUploadSizeMb;
        entity.AllowedAttachmentExtensions = string.Join(',', extensions);
        entity.AllowedProfilePhotoExtensions = string.Join(',', photoExtensions);
        entity.MaxLoginAttempts = settings.MaxLoginAttempts;
        entity.LoginLockoutMinutes = settings.LoginLockoutMinutes;
        entity.OtpExpiryMinutes = settings.OtpExpiryMinutes;
        entity.OtpResendCooldownSeconds = settings.OtpResendCooldownSeconds;
        entity.OtpMaxResendCount = settings.OtpMaxResendCount;
        entity.OtpEmailMessageTemplate = emailTemplate;
        entity.OtpSmsMessageTemplate = smsTemplate;
        entity.TermsAndConditionsText = termsText;
        entity.TermsAgreementLabel = termsAgreementLabel;
        entity.AuditLogRetentionDays = settings.AuditLogRetentionDays;
        entity.NotificationRetentionDays = settings.NotificationRetentionDays;
        entity.AuditLogMaxCount = settings.AuditLogMaxCount;
        entity.NotificationMaxCount = settings.NotificationMaxCount;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheKey);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Settings",
            EntityId = entity.Id,
            Action = "SettingsChanged",
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        return null;
    }

    private static string? ValidateOtpMessageTemplate(string template, int maxLength, string token)
    {
        if (template.Length == 0)
        {
            return "نص رمز التحقق مطلوب";
        }

        if (template.Length > maxLength)
        {
            return $"نص رمز التحقق يجب ألا يتجاوز {maxLength} حرفًا";
        }

        if (TextInput.HasControlCharacters(template))
        {
            return "نص رمز التحقق يحتوي على أحرف غير مسموح بها";
        }

        if (!OtpMessageTemplate.Contains(template, token))
        {
            return "يرجى تحديد مكان وضع رمز التحقق في خانة الرسالة";
        }

        return null;
    }

    private static string? ValidateRequestLimits(SystemSettingsDto settings)
    {
        if (settings.LicenseValidityMonths is < 1 or > MaxLicenseValidityMonths)
        {
            return $"مدة صلاحية الرخصة يجب أن تكون بين 1 و {MaxLicenseValidityMonths} شهرًا";
        }

        if (settings.ResponseSlaBusinessDays is < 1 or > MaxResponseSlaBusinessDays)
        {
            return $"مهلة الرد على الطلب يجب أن تكون بين 1 و {MaxResponseSlaBusinessDays} يوم عمل";
        }

        if (settings.MaxActiveRequestsPerUser is < 1 or > MaxActiveRequestsPerUserCeiling)
        {
            return $"حد عدد الطلبات للمستخدم يجب أن يكون بين 1 و {MaxActiveRequestsPerUserCeiling}";
        }

        if (settings.MaxUploadSizeMb is < 1 or > MaxUploadSizeMbCeiling)
        {
            return $"الحد الأقصى لحجم الملف يجب أن يكون بين 1 و {MaxUploadSizeMbCeiling} ميجابايت";
        }

        return null;
    }

    private static string? ValidateExtensions(
        List<string> extensions, IReadOnlyCollection<string> supportedExtensions, string emptyError, string unsupportedError)
    {
        if (extensions.Count == 0)
        {
            return emptyError;
        }

        if (extensions.Any(extension => !supportedExtensions.Contains(extension)))
        {
            return unsupportedError;
        }

        return null;
    }

    private static string? ValidateLoginAndOtpSettings(SystemSettingsDto settings)
    {
        if (settings.MaxLoginAttempts is < 1 or > MaxLoginAttemptsCeiling)
        {
            return $"عدد محاولات تسجيل الدخول يجب أن يكون بين 1 و {MaxLoginAttemptsCeiling}";
        }

        if (settings.LoginLockoutMinutes is < 1 or > MaxLoginLockoutMinutesCeiling)
        {
            return $"مدة الإيقاف المؤقت يجب أن تكون بين 1 و {MaxLoginLockoutMinutesCeiling} دقيقة";
        }

        if (settings.OtpExpiryMinutes is < 1 or > MaxOtpExpiryMinutesCeiling)
        {
            return $"مدة صلاحية كود التحقق يجب أن تكون بين 1 و {MaxOtpExpiryMinutesCeiling} دقيقة";
        }

        if (settings.OtpResendCooldownSeconds is < 1 or > MaxOtpResendCooldownSecondsCeiling)
        {
            return $"مدة إعادة إرسال رمز التحقق يجب أن تكون بين 1 و {MaxOtpResendCooldownSecondsCeiling} ثانية";
        }

        if (settings.OtpMaxResendCount is < 1 or > MaxOtpResendCountCeiling)
        {
            return $"الحد الأقصى لإعادة إرسال رمز التحقق يجب أن يكون بين 1 و {MaxOtpResendCountCeiling}";
        }

        return null;
    }

    private static string? ValidateRetentionValue(int value, int ceiling, string fieldLabel)
        => value < 0 || value > ceiling
            ? $"{fieldLabel} يجب أن تكون 0 (للتعطيل) أو بين 1 و {ceiling}"
            : null;

    private static string? ValidateRetentionSettings(SystemSettingsDto settings)
    {
        if (ValidateRetentionValue(settings.AuditLogRetentionDays, MaxRetentionDaysCeiling, "مدة الاحتفاظ بسجل العمليات") is { } auditRetentionError)
        {
            return auditRetentionError;
        }

        if (ValidateRetentionValue(settings.NotificationRetentionDays, MaxRetentionDaysCeiling, "مدة الاحتفاظ بإشعارات المستخدمين") is { } notificationRetentionError)
        {
            return notificationRetentionError;
        }

        if (ValidateRetentionValue(settings.AuditLogMaxCount, MaxRetentionCountCeiling, "الحد الأقصى لعدد سجل العمليات") is { } auditMaxCountError)
        {
            return auditMaxCountError;
        }

        if (ValidateRetentionValue(settings.NotificationMaxCount, MaxRetentionCountCeiling, "الحد الأقصى لعدد سجل إشعارات المستخدمين") is { } notificationMaxCountError)
        {
            return notificationMaxCountError;
        }

        return null;
    }

    private static string? ValidateRequiredText(string text, int maxLength, string fieldLabel)
    {
        if (text.Length == 0)
        {
            return $"{fieldLabel} مطلوب";
        }

        if (text.Length > maxLength)
        {
            return $"{fieldLabel} يجب ألا يتجاوز {maxLength} حرفًا";
        }

        if (TextInput.HasControlCharacters(text))
        {
            return $"{fieldLabel} يحتوي على أحرف غير مسموح بها";
        }

        return null;
    }

    private static string[] SplitExtensions(string stored)
        => stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static List<string> NormalizeExtensions(IReadOnlyList<string>? extensions)
        => extensions is null
            ? []
            : extensions
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => extension.Trim().TrimStart('.').ToUpperInvariant())
                .Distinct()
                .ToList();
}
