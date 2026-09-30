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

public class RequestCreationServiceLimitTests
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

    private static RequestCreationDto ValidSubmitDto(Guid systemId) => new()
    {
        FirstName = "أحمد",
        LastName = "علي",
        Email = "ahmed@example.com",
        Mobile = "0512345678",
        UserType = nameof(UserTypeDisplay.Contractor),
        CompanyName = "شركة تجريبية",
        CommercialNo = "1010101010",
        ProjectName = "مشروع تجريبي",
        ContractNumber = "C-001",
        SystemId = systemId,
        AgreedToTerms = true
    };

    private static async Task<Guid> SeedActiveTargetSystemAsync(PmisDbContext context)
    {
        var system = new TargetSystem { SystemName = "Oracle Primavera Unifier", IsActive = true };
        context.TargetSystems.Add(system);
        await context.SaveChangesAsync();
        return system.Id;
    }

    private static (RequestCreationService Service, PmisDbContext Context) BuildService(int maxActiveRequests)
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new PmisDbContext(options);
        var unitOfWork = new UnitOfWork(context);

        var auditLogRepository = new Mock<IAuditLogRepository>();
        var notificationService = new Mock<INotificationService>();
        var systemSettingsService = new Mock<ISystemSettingsService>();
        systemSettingsService
            .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemSettingsDto
            {
                LicenseValidityMonths = 12,
                ResponseSlaBusinessDays = 3,
                MaxActiveRequestsPerUser = maxActiveRequests,
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

        return (service, context);
    }

    private static async Task SeedRequestAsync(PmisDbContext context, Guid userId, RequestStatus status)
    {
        context.Requests.Add(new Request
        {
            RequestNumber = $"REQ-SEED-{Guid.NewGuid():N}",
            UserId = userId,
            Company = new Company(),
            Project = new Project(),
            Status = status
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveDraftAsync_Succeeds_WhenUnderTheLimit()
    {
        var (service, context) = BuildService(maxActiveRequests: 2);
        await SeedRequestAsync(context, Actor.UserId, RequestStatus.Draft);

        var (result, error) = await service.SaveDraftAsync(ValidDraftDto(), Actor);

        Assert.Null(error);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SaveDraftAsync_CountsExistingDrafts_TowardTheLimit()
    {
        var (service, context) = BuildService(maxActiveRequests: 1);
        await SeedRequestAsync(context, Actor.UserId, RequestStatus.Draft);

        var (result, error) = await service.SaveDraftAsync(ValidDraftDto(), Actor);

        Assert.Null(result);
        Assert.Equal("لقد تجاوزت الحد الأقصى المسموح به لعدد الطلبات المفتوحة", error);
    }

    [Theory]
    [InlineData(RequestStatus.Cancelled)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Activated)]
    public async Task SaveDraftAsync_IgnoresTerminalRequests_WhenCountingTowardTheLimit(RequestStatus terminalStatus)
    {
        var (service, context) = BuildService(maxActiveRequests: 1);
        await SeedRequestAsync(context, Actor.UserId, terminalStatus);

        var (result, error) = await service.SaveDraftAsync(ValidDraftDto(), Actor);

        Assert.Null(error);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(RequestStatus.Submitted)]
    [InlineData(RequestStatus.ReturnedForCorrection)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.AwaitingPaymentConfirmation)]
    [InlineData(RequestStatus.PaymentConfirmed)]
    public async Task SubmitAsync_CountsEveryNonTerminalStatus_TowardTheLimit(RequestStatus nonTerminalStatus)
    {
        var (service, context) = BuildService(maxActiveRequests: 1);
        await SeedRequestAsync(context, Actor.UserId, nonTerminalStatus);
        var systemId = await SeedActiveTargetSystemAsync(context);

        var (result, error) = await service.SubmitAsync(ValidSubmitDto(systemId), Actor);

        Assert.Null(result);
        Assert.Equal("لقد تجاوزت الحد الأقصى المسموح به لعدد الطلبات المفتوحة", error);
    }

    [Fact]
    public async Task SaveDraftAsync_OnlyCountsTheCallingUsersOwnRequests()
    {
        var (service, context) = BuildService(maxActiveRequests: 1);
        await SeedRequestAsync(context, Guid.NewGuid(), RequestStatus.Draft);

        var (result, error) = await service.SaveDraftAsync(ValidDraftDto(), Actor);

        Assert.Null(error);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SubmitExistingDraftAsync_IsNotBlockedByTheLimit()
    {
        var (service, context) = BuildService(maxActiveRequests: 1);
        var request = new Request
        {
            RequestNumber = "REQ-SEED-1",
            UserId = Actor.UserId,
            Company = new Company(),
            Project = new Project(),
            Status = RequestStatus.Draft
        };
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var error = await service.SubmitExistingDraftAsync(request.Id, Actor, agreedToTerms: true);

        Assert.Null(error);
    }
}
