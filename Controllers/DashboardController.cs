using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app")]
public class DashboardController(IRequestAggregateQueryService requestAggregateQueryService) : PmisAuthorizedControllerBase
{
    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> DashboardSummary([FromQuery] DashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var summary = await requestAggregateQueryService.GetDashboardSummaryAsync(userId, userRole, query, cancellationToken);
        return new JsonResult(summary, JsonOptions);
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> ReportsSummary([FromQuery] ReportsSummaryQuery query, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        if (userRole is not (UserRole.P3OOfficer or UserRole.SystemAdministrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بعرض هذا التقرير" });
        }

        var summary = await requestAggregateQueryService.GetReportsSummaryAsync(userId, userRole, query, cancellationToken);
        return new JsonResult(summary, JsonOptions);
    }

    [HttpGet("activity/page")]
    public async Task<IActionResult> ActivityPage(int page, int pageSize, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var result = await requestAggregateQueryService.GetActivityPageAsync(
            userId, userRole, page <= 0 ? 1 : page, pageSize <= 0 ? 10 : pageSize, cancellationToken);
        return new JsonResult(result, JsonOptions);
    }

    [HttpGet("dashboard/action-items/page")]
    public async Task<IActionResult> DashboardActionItemsPage(int page, int pageSize, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var result = await requestAggregateQueryService.GetActionItemsPageAsync(
            userId, userRole, page <= 0 ? 1 : page, pageSize <= 0 ? 2 : pageSize, cancellationToken);
        return new JsonResult(result, JsonOptions);
    }

    [HttpGet("dashboard/licenses/page")]
    public async Task<IActionResult> DashboardLicensesPage(int page, int pageSize, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var result = await requestAggregateQueryService.GetLicensesPageAsync(
            userId, userRole, page <= 0 ? 1 : page, pageSize <= 0 ? 2 : pageSize, cancellationToken);
        return new JsonResult(result, JsonOptions);
    }
}
