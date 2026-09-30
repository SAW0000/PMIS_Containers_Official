using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/requests")]
public class RequestsController(
    IRequestQueryService requestQueryService,
    IRequestCreationService requestCreationService) : PmisAuthorizedControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Requests(CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var requests = await requestQueryService.GetVisibleRequestsAsync(userId, userRole, includeDetails: false, cancellationToken: cancellationToken);
        return new JsonResult(requests, JsonOptions);
    }

    [HttpGet("{requestNumber}/detail")]
    public async Task<IActionResult> RequestDetail(string requestNumber, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var detail = await requestQueryService.GetRequestDetailAsync(requestNumber, userId, userRole, cancellationToken);
        return detail is null ? NotFound() : new JsonResult(detail, JsonOptions);
    }

    [HttpGet("page")]
    public async Task<IActionResult> RequestsPage([FromQuery] RequestListQuery query, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var page = await requestQueryService.GetVisibleRequestsPageAsync(userId, userRole, query, cancellationToken);
        return new JsonResult(page, JsonOptions);
    }

    [HttpGet("/app/badges/submitted-count")]
    public async Task<IActionResult> SubmittedCountBadge(CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var count = await requestQueryService.GetSubmittedCountAsync(userId, userRole, cancellationToken);
        return new JsonResult(new { count }, JsonOptions);
    }

    [HttpPost("draft")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveRequestDraft([FromBody] RequestCreationDto dto, CancellationToken cancellationToken)
        => RunCreationActionAsync(actor => requestCreationService.SaveDraftAsync(dto, actor, cancellationToken));

    [HttpPost("submit")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SubmitNewRequest([FromBody] RequestCreationDto dto, CancellationToken cancellationToken)
        => RunCreationActionAsync(actor => requestCreationService.SubmitAsync(dto, actor, cancellationToken));

    [HttpPost("{requestId:guid}/submit-draft")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExistingDraft(Guid requestId, [FromBody] SubmitExistingDraftBody body, CancellationToken cancellationToken)
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

        var error = await requestCreationService.SubmitExistingDraftAsync(requestId, actor, body.AgreedToTerms, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpGet("{requestId:guid}/draft-details")]
    public async Task<IActionResult> GetDraftDetails(Guid requestId, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        var details = await requestCreationService.GetDraftDetailsAsync(requestId, actor.UserId, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        return new JsonResult(details, JsonOptions);
    }

    [HttpPost("{requestId:guid}/cancel-draft")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDraft(Guid requestId, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        if (actor.Role != UserRole.ContractorConsultant)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بحذف هذا الطلب" });
        }

        var error = await requestCreationService.CancelDraftAsync(requestId, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpPost("{requestId:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        if (actor.Role != UserRole.ContractorConsultant)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بإلغاء هذا الطلب" });
        }

        var error = await requestCreationService.CancelRequestAsync(requestId, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpPost("{requestId:guid}/resubmit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResubmitRequest(Guid requestId, [FromBody] RequestCreationDto dto, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        if (actor.Role != UserRole.ContractorConsultant)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بإعادة إرسال هذا الطلب" });
        }

        var error = await requestCreationService.ResubmitAsync(requestId, dto, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }
}

public class SubmitExistingDraftBody
{
    public required bool AgreedToTerms { get; set; }
}
