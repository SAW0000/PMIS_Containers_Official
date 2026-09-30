using Amazon.S3;
using Microsoft.Extensions.Logging;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Application.Services;

public class UserPhotoUploadService(
    IUserPhotoStorage photoStorage,
    IVirusScanner virusScanner,
    IUnitOfWork unitOfWork,
    ILogger<UserPhotoUploadService> logger) : IUserPhotoUploadService
{
    public async Task<(ScannedPhoto? Photo, string? Error)> ValidateAndScanAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();
        try
        {
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            try
            {
                var policy = await GetUploadPolicyAsync(cancellationToken);

                await photoStorage.ValidateAsync(buffer, fileName, contentType, buffer.Length, policy, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return (null, ex.Message);
            }

            var scan = await virusScanner.ScanAsync(buffer, cancellationToken);
            if (scan.Verdict != ScanVerdict.Clean)
            {
                return (null, scan.Verdict == ScanVerdict.Infected
                    ? "تم رفض الصورة — تم اكتشاف محتوى ضار"
                    : "تعذر فحص الصورة، حاول مرة أخرى لاحقًا");
            }

            buffer.Position = 0;
            var scanned = new ScannedPhoto(buffer, fileName, contentType);
            buffer = null;
            return (scanned, null);
        }
        finally
        {
            if (buffer is not null)
            {
                await buffer.DisposeAsync();
            }
        }
    }

    public async Task<string?> AttachToUserAsync(
        ScannedPhoto photo, User user, CancellationToken cancellationToken = default)
    {
        string newKey;
        try
        {
            var policy = await GetUploadPolicyAsync(cancellationToken);
            newKey = await photoStorage.SaveAsync(
                photo.Content, photo.FileName, photo.ContentType, photo.Content.Length, user.Id, policy, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or AmazonS3Exception)
        {
            logger.LogError(ex, "Storing the profile photo for {UserId} failed; the existing photo is kept.", user.Id);
            return null;
        }

        var supersededKey = user.PhotoFileRef;
        user.PhotoFileRef = newKey;

        return string.Equals(supersededKey, newKey, StringComparison.Ordinal) ? null : supersededKey;
    }

    public async Task DeleteSupersededAsync(
        string objectKey, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await photoStorage.DeleteAsync(objectKey, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Failed to delete the superseded profile photo for {UserId}.", userId);
        }
    }

    private async Task<UserPhotoUploadPolicy> GetUploadPolicyAsync(CancellationToken cancellationToken)
    {
        var settings = await unitOfWork.SystemSettings.GetByIdAsync(SystemSettingsConfiguration.SingletonId, cancellationToken)
            ?? throw new InvalidOperationException("SystemSettings singleton row is missing — check the InitialCreate migration's seed data.");

        var allowedExtensions = settings.AllowedProfilePhotoExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new UserPhotoUploadPolicy(allowedExtensions);
    }
}
