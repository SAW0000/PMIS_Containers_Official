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

public class RequestCreationServiceCancelAttachmentCleanupTests
{
    private static readonly RequestActor Actor = new(
        Guid.NewGuid(), "Test Applicant", UserRole.ContractorConsultant, "127.0.0.1");

    private static (RequestCreationService Service, PmisDbContext Context, Mock<IAttachmentStorage> AttachmentStorage) BuildService()
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new PmisDbContext(options);
        var unitOfWork = new UnitOfWork(context);

        var auditLogRepository = new Mock<IAuditLogRepository>();
        var notificationService = new Mock<INotificationService>();
        var systemSettingsService = new Mock<ISystemSettingsService>();
        var attachmentStorage = new Mock<IAttachmentStorage>();
        var logger = new Mock<ILogger<RequestCreationService>>();

        var service = new RequestCreationService(
            context, unitOfWork, auditLogRepository.Object, notificationService.Object, systemSettingsService.Object,
            attachmentStorage.Object, logger.Object);

        return (service, context, attachmentStorage);
    }

    private static async Task<Request> SeedRequestWithAttachmentAsync(
        PmisDbContext context, Guid userId, RequestStatus status, string objectKey)
    {
        var request = new Request
        {
            RequestNumber = $"REQ-SEED-{Guid.NewGuid():N}",
            UserId = userId,
            Company = new Company(),
            Project = new Project(),
            Status = status
        };
        context.Requests.Add(request);
        context.Attachments.Add(new Attachment
        {
            RequestId = request.Id,
            Request = request,
            AttachmentType = AttachmentType.CommercialRegistration,
            FileName = "file.pdf",
            FilePath = objectKey,
            FileSize = 1024,
            FileType = "application/pdf"
        });
        await context.SaveChangesAsync();
        return request;
    }

    [Fact]
    public async Task CancelDraftAsync_DeletesTheAttachmentRowAndItsMinioObject()
    {
        var (service, context, attachmentStorage) = BuildService();
        var request = await SeedRequestWithAttachmentAsync(context, Actor.UserId, RequestStatus.Draft, "requests/r1/file.pdf");

        var error = await service.CancelDraftAsync(request.Id, Actor);

        Assert.Null(error);
        Assert.False(await context.Attachments.AnyAsync(a => a.RequestId == request.Id));
        attachmentStorage.Verify(s => s.DeleteAsync("requests/r1/file.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelDraftAsync_StorageFailure_StillCancelsTheDraft()
    {
        var (service, context, attachmentStorage) = BuildService();
        var request = await SeedRequestWithAttachmentAsync(context, Actor.UserId, RequestStatus.Draft, "requests/r1/file.pdf");
        attachmentStorage
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Amazon.S3.AmazonS3Exception("boom"));

        var error = await service.CancelDraftAsync(request.Id, Actor);

        Assert.Null(error);
        var reloaded = await context.Requests.AsNoTracking().FirstAsync(r => r.Id == request.Id);
        Assert.Equal(RequestStatus.Cancelled, reloaded.Status);
    }

    [Fact]
    public async Task CancelRequestAsync_FromReturnedForCorrection_DeletesAttachments()
    {
        var (service, context, attachmentStorage) = BuildService();
        var request = await SeedRequestWithAttachmentAsync(
            context, Actor.UserId, RequestStatus.ReturnedForCorrection, "requests/r2/file.pdf");

        var error = await service.CancelRequestAsync(request.Id, Actor);

        Assert.Null(error);
        Assert.False(await context.Attachments.AnyAsync(a => a.RequestId == request.Id));
        attachmentStorage.Verify(s => s.DeleteAsync("requests/r2/file.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(RequestStatus.Submitted)]
    [InlineData(RequestStatus.AwaitingPaymentConfirmation)]
    [InlineData(RequestStatus.PaymentConfirmed)]
    public async Task CancelRequestAsync_FromAnyOtherCancellableStatus_LeavesAttachmentsUntouched(RequestStatus status)
    {
        var (service, context, attachmentStorage) = BuildService();
        var request = await SeedRequestWithAttachmentAsync(context, Actor.UserId, status, "requests/r3/file.pdf");

        var error = await service.CancelRequestAsync(request.Id, Actor);

        Assert.Null(error);
        Assert.True(await context.Attachments.AnyAsync(a => a.RequestId == request.Id));
        attachmentStorage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
