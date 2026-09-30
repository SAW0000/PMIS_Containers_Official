using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface ITargetSystemService
{
    Task<IReadOnlyList<TargetSystemAdminDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default);

    Task<(TargetSystemAdminDto? Created, string? Error)> CreateAsync(
        TargetSystemInput input, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> SetActiveAsync(
        Guid id, bool isActive, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default);
}
