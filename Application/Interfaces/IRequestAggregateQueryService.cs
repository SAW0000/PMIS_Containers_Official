using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface IRequestAggregateQueryService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        Guid currentUserId, UserRole currentRole, DashboardSummaryQuery query, CancellationToken cancellationToken = default);

    Task<ReportsSummaryDto> GetReportsSummaryAsync(
        Guid currentUserId, UserRole currentRole, ReportsSummaryQuery query, CancellationToken cancellationToken = default);

    Task<PagedListDto<ActivityEventDto>> GetActivityPageAsync(
        Guid currentUserId, UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedListDto<ActionItemDto>> GetActionItemsPageAsync(
        Guid currentUserId, UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedListDto<LicenseItemDto>> GetLicensesPageAsync(
        Guid currentUserId, UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default);
}
