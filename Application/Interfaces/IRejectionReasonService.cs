using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface IRejectionReasonService
{
    Task<IReadOnlyList<RejectionReasonOptionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(RejectionReasonOptionDto? Created, string? Error)> CreateAsync(
        RejectionReasonOptionInput input, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> UpdateAsync(
        Guid id, RejectionReasonOptionInput input, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default);
}
