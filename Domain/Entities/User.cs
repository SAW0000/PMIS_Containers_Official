using PMIS.Domain.Enums;

namespace PMIS.Domain.Entities;

public class User : Entity
{
    public string FullName { get; set; } = string.Empty;

    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];

    public UserRole? PrimaryRole => RoleAssignments.Select(ra => ra.Role).HighestPriority();

    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? PhotoFileRef { get; set; }

    public string? NationalId { get; set; }

    public string? ArabicFirstName { get; set; }
    public string? ArabicFatherName { get; set; }
    public string? ArabicGrandFatherName { get; set; }

    public string? ArabicFamilyName { get; set; }
    public string? ArabicNationality { get; set; }

    public Gender? Gender { get; set; }

    public bool DeletedAccount { get; set; }

    public ICollection<Request> Requests { get; set; } = [];
}
