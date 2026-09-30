using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Application.Services;

public class RequestAttachmentService(
    PmisDbContext context, IUnitOfWork unitOfWork, IAttachmentStorage attachmentStorage,
    IVirusScanner virusScanner, IAuditLogRepository auditLogRepository,
    ILogger<RequestAttachmentService> logger)
    : IRequestAttachmentService
{
    public async Task<(AttachmentDto? Attachment, string? Error)> UploadAsync(
        Guid requestId, AttachmentType attachmentType, UploadedFile file, RequestActor actor,
        CancellationToken cancellationToken = default)
    {
        var request = await context.Requests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null || !CanAccess(request, actor.UserId, actor.Role))
        {
            return (null, "الطلب غير موجود");
        }

        if (actor.Role != UserRole.ContractorConsultant)
        {
            return (null, "رفع المرفقات متاح لمقدّم الطلب فقط");
        }

        var existing = await context.Attachments
            .FirstOrDefaultAsync(a => a.RequestId == requestId && a.AttachmentType == attachmentType, cancellationToken);
        if (existing is not null)
        {
            await attachmentStorage.DeleteAsync(existing.FilePath, cancellationToken);
            unitOfWork.Attachments.Remove(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var policy = await GetUploadPolicyAsync(cancellationToken);
        var upload = new AttachmentUpload(file.Content, file.FileName, file.ContentType, file.FileSize, requestId, attachmentType);

        try
        {
            await attachmentStorage.ValidateAsync(upload, policy, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return (null, ex.Message);
        }

        var scan = await virusScanner.ScanAsync(file.Content, cancellationToken);
        if (scan.Verdict != ScanVerdict.Clean)
        {
            if (scan.Verdict == ScanVerdict.Infected)
            {
                await LogVirusDetectedAsync(request, attachmentType, scan, actor, cancellationToken);
            }
            else
            {
                await LogRejectedAttachmentAsync(request, attachmentType, scan, actor, cancellationToken);
            }

            return (null, scan.Verdict == ScanVerdict.Infected
                ? "تم رفض الملف — تم اكتشاف محتوى ضار"
                : "تعذر فحص الملف، حاول مرة أخرى لاحقًا");
        }

        string objectKey;
        try
        {
            objectKey = await attachmentStorage.SaveAsync(upload, policy, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return (null, ex.Message);
        }

        var attachment = new Attachment
        {
            RequestId = requestId,
            AttachmentType = attachmentType,
            FileName = file.FileName,
            FilePath = objectKey,
            FileSize = file.FileSize,
            FileType = file.ContentType
        };

        await unitOfWork.Attachments.AddAsync(attachment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Request",
            EntityId = requestId,
            Action = "AttachmentUploaded",
            RequestId = requestId,
            RequestNumberSnapshot = request.RequestNumber,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        return (ToDto(attachment), null);
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListForRequestAsync(
        Guid requestId, Guid currentUserId, UserRole currentRole, CancellationToken cancellationToken = default)
    {
        var request = await context.Requests
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null || !CanAccess(request, currentUserId, currentRole))
        {
            return [];
        }

        return request.Attachments
            .Where(a => a.IsActive)
            .OrderBy(a => a.UploadDate)
            .Select(ToDto)
            .ToList();
    }

    public async Task<(AttachmentDownload? Download, string? Error)> OpenForDownloadAsync(
        Guid attachmentId, Guid currentUserId, UserRole currentRole, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var attachment = await context.Attachments
            .Include(a => a.Request)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

        if (attachment is null || !CanAccess(attachment.Request, currentUserId, currentRole))
        {
            return (null, null);
        }

        var buffer = new MemoryStream();
        await using (var s3Stream = await attachmentStorage.OpenReadAsync(attachment.FilePath, cancellationToken))
        {
            await s3Stream.CopyToAsync(buffer, cancellationToken);
        }
        buffer.Position = 0;

        var scan = await virusScanner.ScanAsync(buffer, cancellationToken);
        if (scan.Verdict != ScanVerdict.Clean)
        {
            if (scan.Verdict == ScanVerdict.Infected)
            {
                await LogVirusDetectedAsync(attachment.Request, attachment.AttachmentType, scan, actor, cancellationToken);
            }
            else
            {
                await LogRejectedAttachmentAsync(attachment.Request, attachment.AttachmentType, scan, actor, cancellationToken);
            }

            await buffer.DisposeAsync();
            return (null, scan.Verdict == ScanVerdict.Infected
                ? "تم حظر تحميل الملف — تم اكتشاف محتوى ضار"
                : "تعذر فحص الملف، حاول مرة أخرى لاحقًا");
        }

        buffer.Position = 0;
        return (new AttachmentDownload(buffer, attachment.FileName, attachment.FileType), null);
    }

    public async Task<string?> DeleteAsync(
        Guid attachmentId, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var attachment = await context.Attachments
            .Include(a => a.Request)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

        if (attachment is null || !CanAccess(attachment.Request, actor.UserId, actor.Role))
        {
            return "المرفق غير موجود";
        }

        if (actor.Role != UserRole.ContractorConsultant)
        {
            return "حذف المرفقات متاح لمقدّم الطلب فقط";
        }

        if (attachment.Request.Status is not (RequestStatus.Draft or RequestStatus.ReturnedForCorrection))
        {
            return "لا يمكن حذف مرفق بعد تقديم الطلب";
        }

        await attachmentStorage.DeleteAsync(attachment.FilePath, cancellationToken);
        unitOfWork.Attachments.Remove(attachment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Request",
            EntityId = attachment.RequestId,
            Action = "AttachmentDeleted",
            RequestId = attachment.RequestId,
            RequestNumberSnapshot = attachment.Request.RequestNumber,
            Note = attachment.AttachmentType.ToString(),
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

    private Task LogVirusDetectedAsync(
        Request request, AttachmentType attachmentType, ScanResult scan, RequestActor actor, CancellationToken cancellationToken)
    {
        var note = $"قام المستخدم {actor.FullName} بإرسال ملف ضمن الطلب {request.RequestNumber} " +
                   $"(نوع المرفق: {attachmentType}) وتم رصده كفيروس بواسطة الفحص الأمني (ClamAV)" +
                   (string.IsNullOrEmpty(scan.Detail) ? string.Empty : $" — {scan.Detail}");

        logger.LogWarning(
            "قام المستخدم {UserFullName} ({UserId}) بإرسال ملف ضمن الطلب {RequestNumber} " +
            "تم رصده كفيروس بواسطة الفحص الأمني (ClamAV) — نوع المرفق: {AttachmentType}",
            actor.FullName, actor.UserId, request.RequestNumber, attachmentType);

        return auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Request",
            EntityId = request.Id,
            Action = "AttachmentRejected",
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            Note = note,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);
    }

    private Task LogRejectedAttachmentAsync(
        Request request, AttachmentType attachmentType, ScanResult scan, RequestActor actor, CancellationToken cancellationToken)
        => auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Request",
            EntityId = request.Id,
            Action = "AttachmentRejected",
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            Note = $"{attachmentType}: {scan.Detail}",
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

    private static bool CanAccess(Request request, Guid currentUserId, UserRole currentRole)
        => currentRole != UserRole.ContractorConsultant || request.UserId == currentUserId;

    private async Task<AttachmentUploadPolicy> GetUploadPolicyAsync(CancellationToken cancellationToken)
    {
        var settings = await unitOfWork.SystemSettings.GetByIdAsync(SystemSettingsConfiguration.SingletonId, cancellationToken)
            ?? throw new InvalidOperationException("SystemSettings singleton row is missing — check the InitialCreate migration's seed data.");

        var allowedExtensions = settings.AllowedAttachmentExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new AttachmentUploadPolicy(settings.MaxUploadSizeMb, allowedExtensions);
    }

    private static AttachmentDto ToDto(Attachment attachment) => new()
    {
        Id = attachment.Id,
        AttachmentType = attachment.AttachmentType.ToString(),
        FileName = attachment.FileName,
        FileSize = attachment.FileSize,
        FileType = attachment.FileType,
        UploadDate = attachment.UploadDate
    };
}
