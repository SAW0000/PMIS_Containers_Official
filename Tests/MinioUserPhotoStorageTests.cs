using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Moq;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Storage;

namespace PMIS.Tests;

public class MinioUserPhotoStorageTests
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] WebPBytes = [0x52, 0x49, 0x46, 0x46, 0x24, 0x8A, 0x01, 0x00, 0x57, 0x45, 0x42, 0x50];
    private static readonly byte[] ExecutableBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00];

    private static readonly UserPhotoUploadPolicy AllExtensionsPolicy = new(["JPG", "JPEG", "PNG", "WEBP"]);

    private const string BucketName = "pmis-attachments";

    private readonly Mock<IAmazonS3> _s3 = new();
    private readonly MinioUserPhotoStorage _storage;

    public MinioUserPhotoStorageTests()
    {
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        var settings = Options.Create(new MinioSettings { BucketName = BucketName });
        _storage = new MinioUserPhotoStorage(_s3.Object, new MinioBucketProvisioner(_s3.Object), settings);
    }

    private void GivenBucketExists()
    {
        _s3.Setup(s => s.ListBucketsAsync(It.IsAny<ListBucketsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse { Buckets = [new S3Bucket { BucketName = BucketName }] });
        _s3.Setup(s => s.GetBucketLocationAsync(It.IsAny<GetBucketLocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetBucketLocationResponse());
    }

    [Fact]
    public async Task ValidateAsync_Passes_ForRealJpeg()
    {
        using var stream = new MemoryStream(JpegBytes);

        await _storage.ValidateAsync(stream, "photo.jpg", "image/jpeg", JpegBytes.Length, AllExtensionsPolicy);
    }

    [Fact]
    public async Task ValidateAsync_Passes_ForRealPng()
    {
        using var stream = new MemoryStream(PngBytes);

        await _storage.ValidateAsync(stream, "photo.png", "image/png", PngBytes.Length, AllExtensionsPolicy);
    }

    [Fact]
    public async Task ValidateAsync_Passes_ForRealWebP()
    {
        using var stream = new MemoryStream(WebPBytes);

        await _storage.ValidateAsync(stream, "photo.webp", "image/webp", WebPBytes.Length, AllExtensionsPolicy);
    }

    [Fact]
    public async Task ValidateAsync_Throws_WhenExecutableIsRenamedToJpg()
    {
        using var stream = new MemoryStream(ExecutableBytes);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.ValidateAsync(stream, "payload.jpg", "image/jpeg", ExecutableBytes.Length, AllExtensionsPolicy));

        Assert.Contains("لا يطابق", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_Throws_WhenExtensionIsNotAnImage()
    {
        using var stream = new MemoryStream(ExecutableBytes);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.ValidateAsync(stream, "payload.exe", "image/jpeg", ExecutableBytes.Length, AllExtensionsPolicy));
    }

    [Fact]
    public async Task ValidateAsync_Throws_WhenContentTypeIsNotAllowed()
    {
        using var stream = new MemoryStream(JpegBytes);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.ValidateAsync(stream, "photo.jpg", "application/octet-stream", JpegBytes.Length, AllExtensionsPolicy));
    }

    [Fact]
    public async Task ValidateAsync_Throws_WhenFileExceedsSizeCap()
    {
        using var stream = new MemoryStream(JpegBytes);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.ValidateAsync(stream, "photo.jpg", "image/jpeg", (5 * 1024 * 1024) + 1, AllExtensionsPolicy));
    }

    [Fact]
    public async Task ValidateAsync_Throws_WhenFileIsEmpty()
    {
        using var stream = new MemoryStream([]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.ValidateAsync(stream, "photo.jpg", "image/jpeg", 0, AllExtensionsPolicy));
    }

    [Fact]
    public async Task ValidateAsync_Throws_WhenExtensionIsNotInAdminPolicy_EvenIfOtherwiseValid()
    {
        using var stream = new MemoryStream(PngBytes);
        var jpgOnlyPolicy = new UserPhotoUploadPolicy(["JPG"]);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.ValidateAsync(stream, "photo.png", "image/png", PngBytes.Length, jpgOnlyPolicy));

        Assert.Contains("غير مسموح به حاليًا", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_Passes_WhenPolicySpellsJpegDifferentlyThanFileExtension()
    {
        using var stream = new MemoryStream(JpegBytes);
        var jpegSpelledPolicy = new UserPhotoUploadPolicy(["JPEG"]);

        await _storage.ValidateAsync(stream, "photo.jpg", "image/jpeg", JpegBytes.Length, jpegSpelledPolicy);
    }

    [Fact]
    public async Task SaveAsync_BuildsKeyScopedToUserWithRandomName()
    {
        GivenBucketExists();
        var userId = Guid.NewGuid();
        using var stream = new MemoryStream(JpegBytes);
        PutObjectRequest? captured = null;
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new PutObjectResponse());

        var key = await _storage.SaveAsync(stream, "my portrait.jpg", "image/jpeg", JpegBytes.Length, userId, AllExtensionsPolicy);

        Assert.StartsWith($"users/{userId}/", key);
        Assert.EndsWith(".jpg", key);
        Assert.DoesNotContain("portrait", key);
        Assert.Equal(BucketName, captured!.BucketName);
        Assert.Equal(key, captured.Key);
    }

    [Fact]
    public async Task SaveAsync_UploadsNothing_WhenValidationFails()
    {
        GivenBucketExists();
        using var stream = new MemoryStream(ExecutableBytes);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.SaveAsync(stream, "payload.jpg", "image/jpeg", ExecutableBytes.Length, Guid.NewGuid(), AllExtensionsPolicy));

        _s3.Verify(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_RewindsStream_SoCompleteContentIsUploaded()
    {
        GivenBucketExists();
        var content = JpegBytes.Concat(new byte[512]).ToArray();
        using var stream = new MemoryStream(content);
        PutObjectRequest? captured = null;
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new PutObjectResponse());

        await _storage.SaveAsync(stream, "photo.jpg", "image/jpeg", content.Length, Guid.NewGuid(), AllExtensionsPolicy);

        Assert.Equal(0, captured!.InputStream.Position);
        Assert.Equal(content.Length, captured.InputStream.Length);
    }
}
