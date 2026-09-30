using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface IUserProfileQueryService
{
    Task<CurrentUserProfileDto?> GetOwnProfileAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken = default);
}
