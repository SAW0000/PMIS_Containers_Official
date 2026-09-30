namespace PMIS.Application.DTOs;

public class CurrentUserProfileDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string FatherName { get; set; } = string.Empty;
    public string GrandFatherName { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public bool HasPhoto { get; set; }
    public string RoleLabel { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public DateTime Joined { get; set; }
    public bool IsActive { get; set; }
}
