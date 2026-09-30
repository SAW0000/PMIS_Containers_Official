using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Repositories;

namespace PMIS.Tests;

public class RequestCreationServiceRequestNumberRetryTests
{
    private static readonly RequestActor Actor = new(
        Guid.NewGuid(), "Test Applicant", UserRole.ContractorConsultant, "127.0.0.1");

    private static RequestCreationDto ValidDraftDto() => new()
    {
        FirstName = "أحمد",
        LastName = "علي",
        Email = "ahmed@example.com",
        Mobile = "0512345678",
        UserType = nameof(UserTypeDisplay.Contractor),
        SystemId = Guid.Empty,
        AgreedToTerms = false
    };

    private static SqlException BuildSqlException(int number, string message)
    {
        var errorCtor = typeof(SqlError).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception)],
            null)!;
        var error = errorCtor.Invoke([number, (byte)0, (byte)14, "test-server", message, "procedure", 1, null]);

        var collection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(collection, [error]);

        var createException = typeof(SqlException).GetMethod(
            "CreateException",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(SqlErrorCollection), typeof(string)],
            null)!;
        return (SqlException)createException.Invoke(null, [collection, "11.0.0"])!;
    }

    private static DbUpdateException RequestNumberCollisionException() => new(
        "duplicate key",
        BuildSqlException(2601,
            "Cannot insert duplicate key row in object 'dbo.Requests' with unique index " +
            "'IX_Requests_RequestNumber'. The duplicate key value is (REQ-2026-0001)."));

    private static DbUpdateException UnrelatedCollisionException() => new(
        "duplicate key",
        BuildSqlException(2601,
            "Cannot insert duplicate key row in object 'dbo.Users' with unique index " +
            "'IX_Users_NationalId'. The duplicate key value is (1234567890)."));

    private class ThrowNTimesUnitOfWork(IUnitOfWork inner, Func<Exception> exceptionFactory, int failuresBeforeSuccess)
        : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public IRepository<User> Users => inner.Users;
        public IRepository<Company> Companies => inner.Companies;
        public IRepository<Project> Projects => inner.Projects;
        public IRepository<TargetSystem> TargetSystems => inner.TargetSystems;
        public IRepository<Request> Requests => inner.Requests;
        public IRepository<Attachment> Attachments => inner.Attachments;
        public IRepository<RequestNote> RequestNotes => inner.RequestNotes;
        public IRepository<RejectionReasonOption> RejectionReasonOptions => inner.RejectionReasonOptions;
        public IRepository<BankAccount> BankAccounts => inner.BankAccounts;
        public IRepository<RequestHistory> RequestHistories => inner.RequestHistories;
        public IRepository<License> Licenses => inner.Licenses;
        public IRepository<SystemSettings> SystemSettings => inner.SystemSettings;
        public IRepository<NotificationPreference> NotificationPreferences => inner.NotificationPreferences;
        public IRepository<UserRoleAssignment> UserRoleAssignments => inner.UserRoleAssignments;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            if (SaveChangesCallCount <= failuresBeforeSuccess)
            {
                throw exceptionFactory();
            }

            return inner.SaveChangesAsync(cancellationToken);
        }
    }

    private static (RequestCreationService Service, PmisDbContext Context, ThrowNTimesUnitOfWork UnitOfWork) BuildService(
        Func<Exception> exceptionFactory, int failuresBeforeSuccess)
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new PmisDbContext(options);
        var realUnitOfWork = new UnitOfWork(context);
        var unitOfWork = new ThrowNTimesUnitOfWork(realUnitOfWork, exceptionFactory, failuresBeforeSuccess);

        var auditLogRepository = new Mock<IAuditLogRepository>();
        var notificationService = new Mock<INotificationService>();
        var systemSettingsService = new Mock<ISystemSettingsService>();
        systemSettingsService
            .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemSettingsDto
            {
                LicenseValidityMonths = 12,
                ResponseSlaBusinessDays = 3,
                MaxActiveRequestsPerUser = 100,
                MaxUploadSizeMb = 20,
                MaxLoginAttempts = 10,
                LoginLockoutMinutes = 2,
                OtpExpiryMinutes = 5,
                OtpResendCooldownSeconds = 45,
                OtpMaxResendCount = 5,
                AuditLogRetentionDays = 0,
                NotificationRetentionDays = 0,
                AuditLogMaxCount = 0,
                NotificationMaxCount = 0
            });

        var attachmentStorage = new Mock<IAttachmentStorage>();
        var logger = new Mock<ILogger<RequestCreationService>>();

        var service = new RequestCreationService(
            context, unitOfWork, auditLogRepository.Object, notificationService.Object, systemSettingsService.Object,
            attachmentStorage.Object, logger.Object);

        return (service, context, unitOfWork);
    }

    [Fact]
    public async Task SaveDraftAsync_RecoversFromASingleRequestNumberCollision()
    {
        var (service, context, unitOfWork) = BuildService(RequestNumberCollisionException, failuresBeforeSuccess: 1);

        var (result, error) = await service.SaveDraftAsync(ValidDraftDto(), Actor);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);

        var saved = await context.Requests.SingleAsync();
        Assert.Equal(result!.RequestNumber, saved.RequestNumber);
        Assert.False(string.IsNullOrWhiteSpace(saved.RequestNumber));
    }

    [Fact]
    public async Task SaveDraftAsync_RecoversFromTwoConsecutiveRequestNumberCollisions()
    {
        var (service, context, unitOfWork) = BuildService(RequestNumberCollisionException, failuresBeforeSuccess: 2);

        var (result, error) = await service.SaveDraftAsync(ValidDraftDto(), Actor);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(3, unitOfWork.SaveChangesCallCount);
        Assert.Single(await context.Requests.ToListAsync());
    }

    [Fact]
    public async Task SaveDraftAsync_GivesUpAfterExhaustingRetryBudget()
    {
        var (service, context, unitOfWork) = BuildService(RequestNumberCollisionException, failuresBeforeSuccess: int.MaxValue);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(
            () => service.SaveDraftAsync(ValidDraftDto(), Actor));

        Assert.IsType<SqlException>(thrown.InnerException);
        Assert.Equal(3, unitOfWork.SaveChangesCallCount);
        Assert.Empty(await context.Requests.ToListAsync());
    }

    [Fact]
    public async Task SaveDraftAsync_DoesNotRetry_WhenTheCollisionIsOnADifferentConstraint()
    {
        var (service, _, unitOfWork) = BuildService(UnrelatedCollisionException, failuresBeforeSuccess: 1);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => service.SaveDraftAsync(ValidDraftDto(), Actor));

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void IsRequestNumberCollision_TrueOnlyForTheRequestNumberUniqueIndexViolation()
    {
        var method = typeof(RequestCreationService).GetMethod(
            "IsRequestNumberCollision", BindingFlags.NonPublic | BindingFlags.Static)!;

        var onRequestNumberIndex = (bool)method.Invoke(null, [RequestNumberCollisionException()])!;
        var onUnrelatedIndex = (bool)method.Invoke(null, [UnrelatedCollisionException()])!;
        var onNonSqlException = (bool)method.Invoke(null, [new DbUpdateException("boom", new InvalidOperationException())])!;

        Assert.True(onRequestNumberIndex);
        Assert.False(onUnrelatedIndex);
        Assert.False(onNonSqlException);
    }
}
