using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface IAdminUserQueryService
{
    Task<PagedAdminUsersDto> GetUsersPageAsync(
        UserRole currentRole, AdminUserListQuery query, CancellationToken cancellationToken = default);
}
