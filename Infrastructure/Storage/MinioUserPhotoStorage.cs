using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Storage;

public class MinioUserPhotoStorage(
    IAmazonS3 s3,
    MinioBucketProvisioner bucketProvisioner,
    IOptions<MinioSettings> settings) : IUserPhotoStorage
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly string _bucketName = settings.Value.BucketName;

    public async Task ValidateAsync(Stream content, string fileName, string contentType, long fileSize, UserPhotoUploadPolicy policy, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new ArgumentException("نوع الملف غير مدعوم — يُسمح فقط بـ JPG وPNG وWEBP");
        }

        if (!IsAllowedByPolicy(extension, policy))
        {
            throw new ArgumentException($"نوع الصورة غير مسموح به حاليًا — الأنواع المسموحة: {string.Join("، ", policy.AllowedExtensions)}");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException("نوع الملف غير مدعوم");
        }

        if (fileSize <= 0 || fileSize > MaxFileSizeBytes)
        {
            throw new ArgumentException("حجم الصورة يجب ألا يتجاوز 5 ميغابايت");
        }

        if (!await FileSignatureValidator.MatchesExtensionAsync(content, extension, cancellationToken))
        {
            throw new ArgumentException("محتوى الملف لا يطابق نوعه المعلن");
        }
    }

    private static bool IsAllowedByPolicy(string extension, UserPhotoUploadPolicy policy)
    {
        var bare = extension.TrimStart('.').ToUpperInvariant();
        var allowed = policy.AllowedExtensions;

        if (bare is "JPG" or "JPEG")
        {
            return allowed.Contains("JPG") || allowed.Contains("JPEG");
        }

        return allowed.Contains(bare);
    }

    public async Task<string> SaveAsync(
        Stream content, string fileName, string contentType, long fileSize, Guid userId,
        UserPhotoUploadPolicy policy, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(content, fileName, contentType, fileSize, policy, cancellationToken);

        await bucketProvisioner.EnsureExistsAsync(_bucketName, cancellationToken);

        var extension = Path.GetExtension(fileName);
        var objectKey = $"users/{userId}/{Guid.NewGuid():N}{extension}";

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);

        return objectKey;
    }

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var response = await s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey
        }, cancellationToken);

        return response.ResponseStream;
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        => s3.DeleteObjectAsync(_bucketName, objectKey, cancellationToken);
}
