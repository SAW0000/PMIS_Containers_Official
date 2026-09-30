using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public class UserPhotoReadService(PmisDbContext context, IUserPhotoStorage photoStorage)
    : IUserPhotoReadService
{
    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    public async Task<UserPhotoDownloadDto?> OpenPhotoAsync(
        Guid targetUserId, Guid requesterId, UserRole requesterRole, CancellationToken cancellationToken = default)
    {
        if (!CanView(targetUserId, requesterId, requesterRole))
        {
            return null;
        }

        var photoFileRef = await context.Users
            .Where(u => u.Id == targetUserId && !u.DeletedAccount)
            .Select(u => u.PhotoFileRef)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(photoFileRef))
        {
            return null;
        }

        if (!ContentTypesByExtension.TryGetValue(Path.GetExtension(photoFileRef), out var contentType))
        {
            return null;
        }

        var buffer = new MemoryStream();
        await using (var objectStream = await photoStorage.OpenReadAsync(photoFileRef, cancellationToken))
        {
            await objectStream.CopyToAsync(buffer, cancellationToken);
        }

        buffer.Position = 0;
        return new UserPhotoDownloadDto(buffer, contentType);
    }

    internal static bool CanView(Guid targetUserId, Guid requesterId, UserRole requesterRole)
    {
        if (requesterId == Guid.Empty)
        {
            return false;
        }

        return targetUserId == requesterId || requesterRole == UserRole.SystemAdministrator;
    }
}
