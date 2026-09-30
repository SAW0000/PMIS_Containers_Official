namespace PMIS.Application.DTOs;

public class AdminUserCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}
