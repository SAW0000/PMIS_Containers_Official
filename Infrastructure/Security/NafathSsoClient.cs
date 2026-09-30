using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Security;

public class NafathSsoClient(HttpClient httpClient, IOptions<NafathSettings> options) : INafathSsoClient
{
    private readonly NafathSettings _settings = options.Value;

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.GetTokenUrl}{_settings.AppKey}";
        var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ResultWrapper<string>>(cancellationToken);
        return result?.Data;
    }

    public async Task<NafathUserClaim?> GetUserClaimAsync(string token, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.UserClaimsUrl}{token}";
        var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ResultWrapper<NafathUserClaim>>(cancellationToken);
        return result?.Data;
    }
}
