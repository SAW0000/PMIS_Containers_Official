using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public record AttachmentUpload(
    Stream Content, string FileName, string ContentType, long FileSize,
    Guid RequestId, AttachmentType AttachmentType);

public record AttachmentUploadPolicy(int MaxFileSizeMb, IReadOnlyCollection<string> AllowedExtensions);

public interface IAttachmentStorage
{
    Task ValidateAsync(AttachmentUpload upload, AttachmentUploadPolicy policy, CancellationToken cancellationToken = default);

    Task<string> SaveAsync(AttachmentUpload upload, AttachmentUploadPolicy policy, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
