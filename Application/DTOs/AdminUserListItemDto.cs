namespace PMIS.Application.DTOs;

public class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];

    public bool Active { get; set; }

    public string Gender { get; set; } = string.Empty;
    public string ArabicNationality { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;

    public string ArabicFirstName { get; set; } = string.Empty;
    public string ArabicFatherName { get; set; } = string.Empty;
    public string ArabicGrandFatherName { get; set; } = string.Empty;
    public string ArabicFamilyName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;

    public bool HasPhoto { get; set; }
}
