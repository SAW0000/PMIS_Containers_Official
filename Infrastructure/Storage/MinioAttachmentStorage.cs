using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Storage;

public class MinioAttachmentStorage : IAttachmentStorage
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "application/pdf", "image/jpeg", "image/png" };

    private readonly IAmazonS3 _s3;
    private readonly MinioBucketProvisioner _bucketProvisioner;
    private readonly string _bucketName;

    public MinioAttachmentStorage(
        IAmazonS3 s3, MinioBucketProvisioner bucketProvisioner, IOptions<MinioSettings> settings)
    {
        _s3 = s3;
        _bucketProvisioner = bucketProvisioner;
        _bucketName = settings.Value.BucketName;
    }

    private const int MaxFileNameLength = 300;

    public async Task ValidateAsync(AttachmentUpload upload, AttachmentUploadPolicy policy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(upload.FileName) || upload.FileName.Length > MaxFileNameLength)
        {
            throw new ArgumentException($"اسم الملف غير صالح أو يتجاوز {MaxFileNameLength} حرفًا");
        }

        if (TextInput.HasControlCharacters(upload.FileName))
        {
            throw new ArgumentException("اسم الملف يحتوي على أحرف غير مسموح بها");
        }

        var extension = Path.GetExtension(upload.FileName);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new ArgumentException("نوع الملف غير مدعوم — يُسمح فقط بـ PDF وJPG وPNG");
        }

        if (!IsAllowedByPolicy(extension, policy))
        {
            throw new ArgumentException($"نوع الملف غير مسموح به حاليًا — الأنواع المسموحة: {string.Join("، ", policy.AllowedExtensions)}");
        }

        if (!AllowedContentTypes.Contains(upload.ContentType))
        {
            throw new ArgumentException("نوع الملف غير مدعوم");
        }

        var maxFileSizeBytes = (long)policy.MaxFileSizeMb * 1024 * 1024;
        if (upload.FileSize <= 0 || upload.FileSize > maxFileSizeBytes)
        {
            throw new ArgumentException($"حجم الملف يجب ألا يتجاوز {policy.MaxFileSizeMb} ميجابايت");
        }

        if (!await FileSignatureValidator.MatchesExtensionAsync(upload.Content, extension, cancellationToken))
        {
            throw new ArgumentException("محتوى الملف لا يطابق نوعه المعلن");
        }
    }

    public async Task<string> SaveAsync(AttachmentUpload upload, AttachmentUploadPolicy policy, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(upload, policy, cancellationToken);

        await _bucketProvisioner.EnsureExistsAsync(_bucketName, cancellationToken);

        var extension = Path.GetExtension(upload.FileName);
        var objectKey = $"requests/{upload.RequestId}/{upload.AttachmentType}/{Guid.NewGuid():N}{extension}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = upload.Content,
            ContentType = upload.ContentType,
            AutoCloseStream = false
        }, cancellationToken);

        return objectKey;
    }

    private static bool IsAllowedByPolicy(string extension, AttachmentUploadPolicy policy)
    {
        var bare = extension.TrimStart('.').ToUpperInvariant();
        var allowed = policy.AllowedExtensions;

        if (bare is "JPG" or "JPEG")
        {
            return allowed.Contains("JPG") || allowed.Contains("JPEG");
        }

        return allowed.Contains(bare);
    }

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var response = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey
        }, cancellationToken);

        return response.ResponseStream;
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        => _s3.DeleteObjectAsync(_bucketName, objectKey, cancellationToken);
}
