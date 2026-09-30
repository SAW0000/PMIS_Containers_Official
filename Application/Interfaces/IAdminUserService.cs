using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface IAdminUserService
{
    Task<string?> UpdateAsync(Guid id, AdminUserUpdateDto dto, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> CreateAsync(AdminUserCreateDto dto, RequestActor actor, CancellationToken cancellationToken = default);
}
