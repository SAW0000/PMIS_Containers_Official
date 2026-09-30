namespace PMIS.Application.DTOs;

public class AdminUserUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;

    public string ArabicFirstName { get; set; } = string.Empty;
    public string ArabicFatherName { get; set; } = string.Empty;
    public string ArabicGrandFatherName { get; set; } = string.Empty;
    public string ArabicFamilyName { get; set; } = string.Empty;
    public string ArabicNationality { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
    public required bool Active { get; set; }
}
