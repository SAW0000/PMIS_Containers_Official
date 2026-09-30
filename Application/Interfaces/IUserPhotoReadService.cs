using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface IUserPhotoReadService
{
    Task<UserPhotoDownloadDto?> OpenPhotoAsync(
        Guid targetUserId, Guid requesterId, UserRole requesterRole, CancellationToken cancellationToken = default);
}
