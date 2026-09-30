using Moq;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Tests;

public class SystemSettingsServiceTermsTests
{
    private static RequestActor Actor() => new(Guid.NewGuid(), "Admin User", UserRole.SystemAdministrator, "127.0.0.1");

    private static SystemSettingsDto ValidSettings() => new()
    {
        LicenseValidityMonths = 12,
        ResponseSlaBusinessDays = 3,
        MaxActiveRequestsPerUser = 5,
        MaxUploadSizeMb = 20,
        AllowedAttachmentExtensions = ["PDF", "JPG", "PNG"],
        AllowedProfilePhotoExtensions = ["JPG", "PNG"],
        MaxLoginAttempts = 10,
        LoginLockoutMinutes = 2,
        OtpExpiryMinutes = 5,
        OtpResendCooldownSeconds = 45,
        OtpMaxResendCount = 5,
        OtpEmailMessageTemplate = "يرجى إدخال رمز التحقق: @MAIL",
        OtpSmsMessageTemplate = "يرجى إدخال رمز التحقق: @SMS",
        TermsAndConditionsText = "بند أول.\nبند ثانٍ.",
        TermsAgreementLabel = "أوافق على الشروط والأحكام.",
        AuditLogRetentionDays = 0,
        NotificationRetentionDays = 0,
        AuditLogMaxCount = 0,
        NotificationMaxCount = 0
    };

    private static (SystemSettingsService Service, Mock<IUnitOfWork> UnitOfWork) BuildService()
    {
        var entity = new SystemSettings { Id = SystemSettingsConfiguration.SingletonId };
        var settingsRepo = new Mock<IRepository<SystemSettings>>();
        settingsRepo.Setup(r => r.GetByIdAsync(SystemSettingsConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.SystemSettings).Returns(settingsRepo.Object);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var auditLog = new Mock<IAuditLogRepository>();
        auditLog.Setup(a => a.AddAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new SystemSettingsService(unitOfWork.Object, auditLog.Object, Mock.Of<IReferenceDataCache>()), unitOfWork);
    }

    [Fact]
    public async Task UpdateAsync_BlankTermsAndConditionsText_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.TermsAndConditionsText = "   ";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("نص الشروط والأحكام مطلوب", error);
    }

    [Fact]
    public async Task UpdateAsync_BlankTermsAgreementLabel_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.TermsAgreementLabel = "";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("نص إقرار الموافقة مطلوب", error);
    }

    [Fact]
    public async Task UpdateAsync_OversizedTermsAndConditionsText_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.TermsAndConditionsText = new string('a', 20_001);

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Contains("20000", error);
    }

    [Fact]
    public async Task UpdateAsync_TermsAndConditionsTextWithControlCharacters_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.TermsAndConditionsText = "بند سليم\0حرف تحكم غير مسموح";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("نص الشروط والأحكام يحتوي على أحرف غير مسموح بها", error);
    }

    [Fact]
    public async Task UpdateAsync_ValidTerms_TrimsAndPersists()
    {
        var (service, unitOfWork) = BuildService();
        var dto = ValidSettings();
        dto.TermsAndConditionsText = "  بند أول.\nبند ثانٍ.  ";
        dto.TermsAgreementLabel = "  أوافق.  ";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Null(error);
        var entity = await unitOfWork.Object.SystemSettings.GetByIdAsync(SystemSettingsConfiguration.SingletonId);
        Assert.Equal("بند أول.\nبند ثانٍ.", entity!.TermsAndConditionsText);
        Assert.Equal("أوافق.", entity.TermsAgreementLabel);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_RoundTripsTermsFields()
    {
        var entity = new SystemSettings
        {
            Id = SystemSettingsConfiguration.SingletonId,
            AllowedAttachmentExtensions = "PDF,JPG,PNG",
            AllowedProfilePhotoExtensions = "JPG,PNG",
            TermsAndConditionsText = "بند أول.",
            TermsAgreementLabel = "أوافق."
        };
        var settingsRepo = new Mock<IRepository<SystemSettings>>();
        settingsRepo.Setup(r => r.GetByIdAsync(SystemSettingsConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.SystemSettings).Returns(settingsRepo.Object);
        var service = new SystemSettingsService(unitOfWork.Object, Mock.Of<IAuditLogRepository>(), Mock.Of<IReferenceDataCache>());

        var result = await service.GetAsync();

        Assert.Equal("بند أول.", result.TermsAndConditionsText);
        Assert.Equal("أوافق.", result.TermsAgreementLabel);
    }
}
