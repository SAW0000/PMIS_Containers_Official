using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Persistence.Configurations;
using PMIS.Infrastructure.Repositories;
using PMIS.Infrastructure.Security;

namespace PMIS.Tests;

public class RequestAttachmentServiceVirusDetectionTests
{
    private const string EicarSignature =
        "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    private static readonly RequestActor Actor = new(
        Guid.NewGuid(), "أحمد الاختبار", UserRole.ContractorConsultant, "127.0.0.1");

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }

    private static (RequestAttachmentService Service, PmisDbContext Context, Mock<IAuditLogRepository> AuditLog, CapturingLogger<RequestAttachmentService> Logger, Guid RequestId, string RequestNumber)
        BuildService()
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new PmisDbContext(options);

        context.SystemSettings.Add(new SystemSettings { Id = SystemSettingsConfiguration.SingletonId });

        var requestId = Guid.NewGuid();
        var requestNumber = "REQ-2026-9999";
        context.Requests.Add(new Request
        {
            Id = requestId,
            RequestNumber = requestNumber,
            UserId = Actor.UserId,
            CompanyId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        });
        context.SaveChanges();

        var unitOfWork = new UnitOfWork(context);

        var attachmentStorage = new Mock<IAttachmentStorage>();
        attachmentStorage
            .Setup(s => s.ValidateAsync(It.IsAny<AttachmentUpload>(), It.IsAny<AttachmentUploadPolicy>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        attachmentStorage
            .Setup(s => s.SaveAsync(It.IsAny<AttachmentUpload>(), It.IsAny<AttachmentUploadPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("requests/test-object-key.pdf");

        var virusScanner = new ClamAvScanner(
            Options.Create(new ClamAvSettings { Host = "127.0.0.1", Port = 3310 }),
            NullLoggerLike<ClamAvScanner>());

        var auditLogRepository = new Mock<IAuditLogRepository>();
        auditLogRepository
            .Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new CapturingLogger<RequestAttachmentService>();

        var service = new RequestAttachmentService(context, unitOfWork, attachmentStorage.Object, virusScanner, auditLogRepository.Object, logger);

        return (service, context, auditLogRepository, logger, requestId, requestNumber);

        static ILogger<T> NullLoggerLike<T>() => Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
    }

    [Fact]
    public async Task UploadAsync_RealEicarSignature_RejectsFile_NeverSavesAttachment()
    {
        var (service, context, _, _, requestId, _) = BuildService();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(EicarSignature));
        var file = new UploadedFile(stream, "eicar.pdf", "application/pdf", stream.Length);

        var (attachment, error) = await service.UploadAsync(requestId, AttachmentType.CompanyLogo, file, Actor);

        Assert.Null(attachment);
        Assert.Equal("تم رفض الملف — تم اكتشاف محتوى ضار", error);
        Assert.Empty(context.Attachments);
    }

    [Fact]
    public async Task UploadAsync_RealEicarSignature_WritesArabicAuditNoteNamingTheUser()
    {
        var (service, _, auditLogRepository, _, requestId, requestNumber) = BuildService();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(EicarSignature));
        var file = new UploadedFile(stream, "eicar.pdf", "application/pdf", stream.Length);

        await service.UploadAsync(requestId, AttachmentType.CompanyLogo, file, Actor);

        auditLogRepository.Verify(r => r.AddAsync(
            It.Is<AuditLogEntry>(e =>
                e.Action == "AttachmentRejected" &&
                e.RequestNumberSnapshot == requestNumber &&
                e.Note != null && e.Note.Contains(Actor.FullName, StringComparison.Ordinal) &&
                e.Note.Contains("رصده كفيروس", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_RealEicarSignature_LogsArabicWarningToIlogger()
    {
        var (service, _, _, logger, requestId, requestNumber) = BuildService();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(EicarSignature));
        var file = new UploadedFile(stream, "eicar.pdf", "application/pdf", stream.Length);

        await service.UploadAsync(requestId, AttachmentType.CompanyLogo, file, Actor);

        Assert.Contains(logger.Warnings, w =>
            w.Contains(Actor.FullName, StringComparison.Ordinal) &&
            w.Contains(requestNumber, StringComparison.Ordinal) &&
            w.Contains("رصده كفيروس", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UploadAsync_OrdinaryCleanFile_Succeeds()
    {
        var (service, context, _, _, requestId, _) = BuildService();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nordinary clean content\n%%EOF"));
        var file = new UploadedFile(stream, "logo.pdf", "application/pdf", stream.Length);

        var (attachment, error) = await service.UploadAsync(requestId, AttachmentType.CompanyLogo, file, Actor);

        Assert.Null(error);
        Assert.NotNull(attachment);
        Assert.Single(context.Attachments);
    }
}
