using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/admin/settings")]
public class AdminSettingsController(
    ISystemSettingsService systemSettingsService,
    ILogRetentionService logRetentionService) : PmisAuthorizedControllerBase
{
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings([FromBody] SystemSettingsDto settings, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بتعديل إعدادات النظام" });
        }

        var error = await systemSettingsService.UpdateAsync(settings, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpPost("audit-log/clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearAuditLog(CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بحذف سجل العمليات" });
        }

        await logRetentionService.ClearAuditLogAsync(actor, cancellationToken);
        return Ok();
    }

    [HttpPost("notifications/clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearNotificationLog(CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بحذف سجل الإشعارات" });
        }

        await logRetentionService.ClearNotificationsAsync(actor, cancellationToken);
        return Ok();
    }
}
