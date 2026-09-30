namespace PMIS.Application.Interfaces;

using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

public interface IAuditLogQueryService
{
    Task<AuditLogPageDto> GetPagedAsync(
        UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default);
}
