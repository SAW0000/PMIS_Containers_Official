using PMIS.Domain.Entities;

namespace PMIS.Application.Interfaces;

public interface IUserPhotoUploadService
{
    Task<(ScannedPhoto? Photo, string? Error)> ValidateAndScanAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    Task<string?> AttachToUserAsync(ScannedPhoto photo, User user, CancellationToken cancellationToken = default);

    Task DeleteSupersededAsync(string objectKey, Guid userId, CancellationToken cancellationToken = default);
}

public sealed record ScannedPhoto(MemoryStream Content, string FileName, string ContentType) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
