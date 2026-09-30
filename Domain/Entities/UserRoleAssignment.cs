using PMIS.Domain.Enums;

namespace PMIS.Domain.Entities;

public class UserRoleAssignment : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public UserRole Role { get; set; }
}
