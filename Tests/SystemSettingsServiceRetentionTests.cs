using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Persistence.Configurations;
using PMIS.Infrastructure.Repositories;

namespace PMIS.Tests;

public class SystemSettingsServiceRetentionTests
{
    private static readonly RequestActor Actor = new(
        Guid.NewGuid(), "Admin", UserRole.SystemAdministrator, "127.0.0.1");

    private static SystemSettingsDto ValidSettings(
        int auditLogRetentionDays = 0,
        int notificationRetentionDays = 0,
        int auditLogMaxCount = 0,
        int notificationMaxCount = 0) => new()
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
        AuditLogRetentionDays = auditLogRetentionDays,
        NotificationRetentionDays = notificationRetentionDays,
        AuditLogMaxCount = auditLogMaxCount,
        NotificationMaxCount = notificationMaxCount
    };

    private static async Task<(SystemSettingsService Service, PmisDbContext Context)> BuildServiceAsync()
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new PmisDbContext(options);

        context.SystemSettings.Add(new SystemSettings { Id = SystemSettingsConfiguration.SingletonId });
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var auditLogRepository = new NoOpAuditLogRepository();
        var service = new SystemSettingsService(unitOfWork, auditLogRepository, new NoOpReferenceDataCache());
        return (service, context);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3650)]
    public async Task UpdateAsync_Accepts_AuditLogRetentionDays_ZeroOrInRange(int validValue)
    {
        var (service, context) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(auditLogRetentionDays: validValue), Actor);

        Assert.Null(error);
        var saved = await context.SystemSettings.FindAsync(SystemSettingsConfiguration.SingletonId);
        Assert.Equal(validValue, saved!.AuditLogRetentionDays);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3651)]
    public async Task UpdateAsync_Rejects_AuditLogRetentionDays_OutOfRange(int invalidValue)
    {
        var (service, _) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(auditLogRetentionDays: invalidValue), Actor);

        Assert.Equal("مدة الاحتفاظ بسجل العمليات يجب أن تكون 0 (للتعطيل) أو بين 1 و 3650", error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3650)]
    public async Task UpdateAsync_Accepts_NotificationRetentionDays_ZeroOrInRange(int validValue)
    {
        var (service, context) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(notificationRetentionDays: validValue), Actor);

        Assert.Null(error);
        var saved = await context.SystemSettings.FindAsync(SystemSettingsConfiguration.SingletonId);
        Assert.Equal(validValue, saved!.NotificationRetentionDays);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3651)]
    public async Task UpdateAsync_Rejects_NotificationRetentionDays_OutOfRange(int invalidValue)
    {
        var (service, _) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(notificationRetentionDays: invalidValue), Actor);

        Assert.Equal("مدة الاحتفاظ بإشعارات المستخدمين يجب أن تكون 0 (للتعطيل) أو بين 1 و 3650", error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10_000_000)]
    public async Task UpdateAsync_Accepts_AuditLogMaxCount_ZeroOrInRange(int validValue)
    {
        var (service, context) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(auditLogMaxCount: validValue), Actor);

        Assert.Null(error);
        var saved = await context.SystemSettings.FindAsync(SystemSettingsConfiguration.SingletonId);
        Assert.Equal(validValue, saved!.AuditLogMaxCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000_001)]
    public async Task UpdateAsync_Rejects_AuditLogMaxCount_OutOfRange(int invalidValue)
    {
        var (service, _) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(auditLogMaxCount: invalidValue), Actor);

        Assert.Equal("الحد الأقصى لعدد سجل العمليات يجب أن تكون 0 (للتعطيل) أو بين 1 و 10000000", error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10_000_000)]
    public async Task UpdateAsync_Accepts_NotificationMaxCount_ZeroOrInRange(int validValue)
    {
        var (service, context) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(notificationMaxCount: validValue), Actor);

        Assert.Null(error);
        var saved = await context.SystemSettings.FindAsync(SystemSettingsConfiguration.SingletonId);
        Assert.Equal(validValue, saved!.NotificationMaxCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000_001)]
    public async Task UpdateAsync_Rejects_NotificationMaxCount_OutOfRange(int invalidValue)
    {
        var (service, _) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(notificationMaxCount: invalidValue), Actor);

        Assert.Equal("الحد الأقصى لعدد سجل إشعارات المستخدمين يجب أن تكون 0 (للتعطيل) أو بين 1 و 10000000", error);
    }

    private sealed class NoOpAuditLogRepository : IAuditLogRepository
    {
        public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
            int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AuditLogEntry>>([]);

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0L);

        public Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0L);

        public Task<long> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
            => Task.FromResult(0L);

        public Task<long> DeleteExcessAsync(int keepCount, CancellationToken cancellationToken = default)
            => Task.FromResult(0L);
    }

    private sealed class NoOpReferenceDataCache : IReferenceDataCache
    {
        public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl) => Task.CompletedTask;

        public Task InvalidateAsync(string key) => Task.CompletedTask;
    }
}
