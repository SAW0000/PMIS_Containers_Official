using Microsoft.Extensions.Options;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Security;

public class NafathBypassSsoClient(IOptions<NafathSettings> options) : INafathSsoClient
{
    private readonly NafathSettings _settings = options.Value;

    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>($"nafath-bypass-{Guid.NewGuid():N}");

    public Task<NafathUserClaim?> GetUserClaimAsync(string token, CancellationToken cancellationToken = default)
    {
        var claim = new NafathUserClaim
        {
            NameIdentifier = _settings.BypassNationalId ?? string.Empty,
            ArabicName = "مستخدم تجاوز نفاذ",
            ArabicFirstName = "تجاوز",
            ArabicFamilyName = "نفاذ"
        };
        return Task.FromResult<NafathUserClaim?>(claim);
    }
}
