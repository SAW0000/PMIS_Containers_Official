using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface IRequestQueryService
{
    Task<IReadOnlyList<RequestListItemDto>> GetVisibleRequestsAsync(
        Guid currentUserId, UserRole currentRole, bool includeDetails = true, CancellationToken cancellationToken = default);

    Task<RequestListItemDto?> GetRequestDetailAsync(
        string requestNumber, Guid currentUserId, UserRole currentRole, CancellationToken cancellationToken = default);

    Task<PagedRequestsDto> GetVisibleRequestsPageAsync(
        Guid currentUserId, UserRole currentRole, RequestListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TargetSystemDto>> GetActiveSystemsAsync(CancellationToken cancellationToken = default);

    Task<int> GetPaymentConfirmationNeededCountAsync(CancellationToken cancellationToken = default);

    Task<int> GetOverdueRequestCountAsync(CancellationToken cancellationToken = default);

    Task<int> GetSubmittedCountAsync(Guid currentUserId, UserRole currentRole, CancellationToken cancellationToken = default);
}
