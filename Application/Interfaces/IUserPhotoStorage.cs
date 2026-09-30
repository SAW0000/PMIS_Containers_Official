namespace PMIS.Application.Interfaces;

public record UserPhotoUploadPolicy(IReadOnlyCollection<string> AllowedExtensions);

public interface IUserPhotoStorage
{
    Task ValidateAsync(Stream content, string fileName, string contentType, long fileSize, UserPhotoUploadPolicy policy, CancellationToken cancellationToken = default);

    Task<string> SaveAsync(Stream content, string fileName, string contentType, long fileSize, Guid userId, UserPhotoUploadPolicy policy, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
