using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;

namespace PMIS.Infrastructure.Queries;

public class AuditLogQueryService(IAuditLogRepository repository) : IAuditLogQueryService
{
    public async Task<AuditLogPageDto> GetPagedAsync(
        UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (currentRole != UserRole.SystemAdministrator)
        {
            return new AuditLogPageDto();
        }

        var pageClamped = Math.Max(1, page);
        var pageSizeClamped = Math.Clamp(pageSize, 1, 100);

        var entries = await repository.GetPagedAsync(pageClamped, pageSizeClamped, cancellationToken);
        var totalCount = await repository.CountAsync(cancellationToken);

        return new AuditLogPageDto
        {
            Items = entries.Select(ToRow).ToList(),
            TotalCount = (int)totalCount
        };
    }

    private static List<string> ToRow(AuditLogEntry e) =>
    [
        e.Timestamp.ToString("yyyy-MM-dd HH:mm"),
        e.PerformedBy.FullNameSnapshot,
        ActionLabel(e.Action),
        e.RequestNumberSnapshot ?? "—",
        e.IpAddress
    ];

    private static string ActionLabel(string action) => AuditActionLabels.For(action);
}
