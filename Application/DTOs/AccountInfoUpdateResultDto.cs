namespace PMIS.Application.DTOs;

public class AccountInfoUpdateResultDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public bool HasPhoto { get; set; }

    public bool IdentityRefreshed { get; set; }
}
