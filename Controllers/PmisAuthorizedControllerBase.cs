using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Authorize]
public abstract class PmisAuthorizedControllerBase : Controller
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    protected const string ForbiddenActionError = "غير مصرح لك بتنفيذ هذا الإجراء";

    protected RequestActor? TryGetActor()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var userRole))
        {
            return null;
        }

        var fullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        return new RequestActor(userId, fullName, userRole, ipAddress);
    }

    protected async Task<IActionResult> RunReviewActionAsync(Func<RequestActor, Task<string?>> action)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.P3OOfficer)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var error = await action(actor);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    protected async Task<IActionResult> RunCreationActionAsync(
        Func<RequestActor, Task<(Application.DTOs.RequestCreationResultDto? Result, string? Error)>> action)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        if (actor.Role != UserRole.ContractorConsultant)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بتقديم طلب" });
        }

        var (result, error) = await action(actor);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(result);
    }
}
