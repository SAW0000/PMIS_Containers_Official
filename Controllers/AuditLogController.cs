using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/audit-log")]
public class AuditLogController(IAuditLogQueryService auditLogQueryService) : PmisAuthorizedControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> AuditLog(int page, int pageSize, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var result = await auditLogQueryService.GetPagedAsync(
            userRole, page <= 0 ? 1 : page, pageSize <= 0 ? 10 : pageSize, cancellationToken);
        return new JsonResult(result, JsonOptions);
    }
}
