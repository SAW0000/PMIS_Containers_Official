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

public class SystemSettingsServiceLimitTests
{
    private static readonly RequestActor Actor = new(
        Guid.NewGuid(), "Admin", UserRole.SystemAdministrator, "127.0.0.1");

    private static SystemSettingsDto ValidSettings(int maxActiveRequestsPerUser) => new()
    {
        LicenseValidityMonths = 12,
        ResponseSlaBusinessDays = 3,
        MaxActiveRequestsPerUser = maxActiveRequestsPerUser,
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
    [InlineData(-1)]
    [InlineData(101)]
    public async Task UpdateAsync_Rejects_MaxActiveRequestsPerUser_OutOfRange(int invalidValue)
    {
        var (service, _) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(invalidValue), Actor);

        Assert.Equal("حد عدد الطلبات للمستخدم يجب أن يكون بين 1 و 100", error);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task UpdateAsync_Accepts_MaxActiveRequestsPerUser_InRange(int validValue)
    {
        var (service, context) = await BuildServiceAsync();

        var error = await service.UpdateAsync(ValidSettings(validValue), Actor);

        Assert.Null(error);
        var saved = await context.SystemSettings.FindAsync(SystemSettingsConfiguration.SingletonId);
        Assert.Equal(validValue, saved!.MaxActiveRequestsPerUser);
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
