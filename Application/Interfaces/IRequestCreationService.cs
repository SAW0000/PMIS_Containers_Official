using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface IRequestCreationService
{
    Task<(RequestCreationResultDto? Result, string? Error)> SaveDraftAsync(
        RequestCreationDto dto, RequestActor actor, CancellationToken cancellationToken = default);

    Task<(RequestCreationResultDto? Result, string? Error)> SubmitAsync(
        RequestCreationDto dto, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> SubmitExistingDraftAsync(Guid requestId, RequestActor actor, bool agreedToTerms, CancellationToken cancellationToken = default);

    Task<RequestDraftDetailsDto?> GetDraftDetailsAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default);

    Task<string?> CancelDraftAsync(Guid requestId, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> CancelRequestAsync(Guid requestId, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> ResubmitAsync(Guid requestId, RequestCreationDto dto, RequestActor actor, CancellationToken cancellationToken = default);
}
