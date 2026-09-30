using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/reports")]
public class ReportsController(IReportExportService reportExportService) : PmisAuthorizedControllerBase
{
    [HttpGet("export")]
    public async Task<IActionResult> ExportReport(ReportPeriod period, ReportExportFormat format, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "قيمة الفترة أو صيغة التصدير غير صحيحة" });
        }

        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        if (actor.Role is not (UserRole.P3OOfficer or UserRole.SystemAdministrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بتصدير هذا التقرير" });
        }

        var file = await reportExportService.ExportAsync(actor.UserId, actor.Role, period, format, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
