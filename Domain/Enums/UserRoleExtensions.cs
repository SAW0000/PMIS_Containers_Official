namespace PMIS.Domain.Enums;

public static class UserRoleExtensions
{
    private static readonly Dictionary<UserRole, int> Priority = new()
    {
        [UserRole.SystemAdministrator] = 0,
        [UserRole.P3OOfficer] = 1,
        [UserRole.ContractorConsultant] = 2
    };

    public static UserRole? HighestPriority(this IEnumerable<UserRole> roles)
        => roles.OrderBy(r => Priority.GetValueOrDefault(r, int.MaxValue)).Cast<UserRole?>().FirstOrDefault();
}
