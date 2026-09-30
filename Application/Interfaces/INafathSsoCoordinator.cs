using PMIS.Application.DTOs;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Interfaces;

public interface INafathSsoCoordinator
{
    Task<string?> GetOneTimeTokenAsync(CancellationToken cancellationToken = default);

    Task StoreRoleForTokenAsync(string token, UserRole role, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task<UserRole?> ConsumeRoleForTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<NafathUserClaim?> GetUserClaimAsync(string token, CancellationToken cancellationToken = default);

    Task StorePendingProfileAsync(string token, Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task<Guid?> ConsumePendingProfileAsync(string token, CancellationToken cancellationToken = default);

    NafathSettings Settings { get; }
}
