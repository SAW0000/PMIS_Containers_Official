namespace PMIS.Infrastructure.Persistence;

public class NafathSettings
{
    public string IamSsoUrl { get; set; } = string.Empty;

    public string GetTokenUrl { get; set; } = string.Empty;

    public string UserClaimsUrl { get; set; } = string.Empty;

    public string AppKey { get; set; } = string.Empty;

    public string IamLoginUrl { get; set; } = string.Empty;

    public string IamLogoutUrl { get; set; } = string.Empty;

    public string? BypassNationalId { get; set; }
}
