using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface INafathSsoClient
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);

    Task<NafathUserClaim?> GetUserClaimAsync(string token, CancellationToken cancellationToken = default);
}
