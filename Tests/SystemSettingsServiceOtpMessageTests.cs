using Moq;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Tests;

public class SystemSettingsServiceOtpMessageTests
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
        TermsAndConditionsText = "بند أول.",
        TermsAgreementLabel = "أوافق.",
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
    public async Task UpdateAsync_BlankEmailTemplate_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.OtpEmailMessageTemplate = "   ";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("نص رمز التحقق مطلوب", error);
    }

    [Fact]
    public async Task UpdateAsync_BlankSmsTemplate_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.OtpSmsMessageTemplate = "";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("نص رمز التحقق مطلوب", error);
    }

    [Theory]
    [InlineData("لا يوجد رمز هنا")]
    [InlineData("@SMS بدل الرمز")] 
    public async Task UpdateAsync_EmailTemplateMissingToken_ReturnsError(string invalidTemplate)
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.OtpEmailMessageTemplate = invalidTemplate;

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("يرجى تحديد مكان وضع رمز التحقق في خانة الرسالة", error);
    }

    [Theory]
    [InlineData("لا يوجد رمز هنا")]
    [InlineData("@MAIL بدل الرمز")] 
    public async Task UpdateAsync_SmsTemplateMissingToken_ReturnsError(string invalidTemplate)
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.OtpSmsMessageTemplate = invalidTemplate;

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("يرجى تحديد مكان وضع رمز التحقق في خانة الرسالة", error);
    }

    [Fact]
    public async Task UpdateAsync_EmailTemplateLowercaseToken_IsAccepted()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.OtpEmailMessageTemplate = "رمزك هو @mail";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Null(error);
    }

    [Fact]
    public async Task UpdateAsync_OversizedSmsTemplate_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.OtpSmsMessageTemplate = new string('ا', 66) + " @SMS"; 

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("نص رمز التحقق يجب ألا يتجاوز 70 حرفًا", error);
    }

    [Fact]
    public async Task UpdateAsync_SmsTemplateWithControlCharacters_ReturnsError()
    {
        var (service, _) = BuildService();
        var dto = ValidSettings();
        dto.OtpSmsMessageTemplate = "رمزك @SMS\0حرف تحكم";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Equal("نص رمز التحقق يحتوي على أحرف غير مسموح بها", error);
    }

    [Fact]
    public async Task UpdateAsync_ValidTemplates_TrimsAndPersists()
    {
        var (service, unitOfWork) = BuildService();
        var dto = ValidSettings();
        dto.OtpEmailMessageTemplate = "  رمزك هو @MAIL  ";
        dto.OtpSmsMessageTemplate = "  رمزك هو @SMS  ";

        var error = await service.UpdateAsync(dto, Actor());

        Assert.Null(error);
        var entity = await unitOfWork.Object.SystemSettings.GetByIdAsync(SystemSettingsConfiguration.SingletonId);
        Assert.Equal("رمزك هو @MAIL", entity!.OtpEmailMessageTemplate);
        Assert.Equal("رمزك هو @SMS", entity.OtpSmsMessageTemplate);
    }

    [Fact]
    public async Task GetAsync_RoundTripsOtpMessageTemplateFields()
    {
        var entity = new SystemSettings
        {
            Id = SystemSettingsConfiguration.SingletonId,
            AllowedAttachmentExtensions = "PDF,JPG,PNG",
            AllowedProfilePhotoExtensions = "JPG,PNG",
            OtpEmailMessageTemplate = "رمزك هو @MAIL",
            OtpSmsMessageTemplate = "رمزك هو @SMS"
        };
        var settingsRepo = new Mock<IRepository<SystemSettings>>();
        settingsRepo.Setup(r => r.GetByIdAsync(SystemSettingsConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.SystemSettings).Returns(settingsRepo.Object);
        var service = new SystemSettingsService(unitOfWork.Object, Mock.Of<IAuditLogRepository>(), Mock.Of<IReferenceDataCache>());

        var result = await service.GetAsync();

        Assert.Equal("رمزك هو @MAIL", result.OtpEmailMessageTemplate);
        Assert.Equal("رمزك هو @SMS", result.OtpSmsMessageTemplate);
    }
}
