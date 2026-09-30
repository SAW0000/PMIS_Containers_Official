using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public record AttachmentDownload(Stream Content, string FileName, string ContentType);

public record RequestActor(Guid UserId, string FullName, UserRole Role, string IpAddress);

public record UploadedFile(Stream Content, string FileName, string ContentType, long FileSize);

public interface IRequestAttachmentService
{
    Task<(AttachmentDto? Attachment, string? Error)> UploadAsync(
        Guid requestId, AttachmentType attachmentType, UploadedFile file, RequestActor actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttachmentDto>> ListForRequestAsync(
        Guid requestId, Guid currentUserId, UserRole currentRole, CancellationToken cancellationToken = default);

    Task<(AttachmentDownload? Download, string? Error)> OpenForDownloadAsync(
        Guid attachmentId, Guid currentUserId, UserRole currentRole, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> DeleteAsync(
        Guid attachmentId, RequestActor actor, CancellationToken cancellationToken = default);
}
