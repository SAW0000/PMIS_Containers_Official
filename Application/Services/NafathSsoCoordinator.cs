using Microsoft.Extensions.Options;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public class NafathSsoCoordinator(
    INafathSsoClient nafathSsoClient,
    INafathLoginStateStore nafathLoginStateStore,
    INafathPendingProfileStore nafathPendingProfileStore,
    IOptions<NafathSettings> nafathOptions) : INafathSsoCoordinator
{
    public NafathSettings Settings { get; } = nafathOptions.Value;

    public Task<string?> GetOneTimeTokenAsync(CancellationToken cancellationToken = default)
        => nafathSsoClient.GetTokenAsync(cancellationToken);

    public Task StoreRoleForTokenAsync(string token, UserRole role, TimeSpan lifetime, CancellationToken cancellationToken = default)
        => nafathLoginStateStore.SetAsync(token, role, lifetime, cancellationToken);

    public Task<UserRole?> ConsumeRoleForTokenAsync(string token, CancellationToken cancellationToken = default)
        => nafathLoginStateStore.GetAndRemoveAsync(token, cancellationToken);

    public Task<NafathUserClaim?> GetUserClaimAsync(string token, CancellationToken cancellationToken = default)
        => nafathSsoClient.GetUserClaimAsync(token, cancellationToken);

    public Task StorePendingProfileAsync(string token, Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default)
        => nafathPendingProfileStore.SetAsync(token, userId, lifetime, cancellationToken);

    public Task<Guid?> ConsumePendingProfileAsync(string token, CancellationToken cancellationToken = default)
        => nafathPendingProfileStore.GetAndRemoveAsync(token, cancellationToken);
}
