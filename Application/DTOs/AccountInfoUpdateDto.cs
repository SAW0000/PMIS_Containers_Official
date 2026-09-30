namespace PMIS.Application.DTOs;

public class AccountInfoUpdateDto
{
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;

    public string? EmailCode { get; set; }

    public string? MobileCode { get; set; }

    public required bool RemovePhoto { get; set; }
}
