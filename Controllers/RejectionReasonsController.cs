using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/rejection-reasons")]
public class RejectionReasonsController(IRejectionReasonService rejectionReasonService) : PmisAuthorizedControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> RejectionReasons(CancellationToken cancellationToken)
        => Ok(await rejectionReasonService.GetAllAsync(cancellationToken));

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRejectionReason(
        [FromBody] RejectionReasonOptionInput body, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.P3OOfficer)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var (created, error) = await rejectionReasonService.CreateAsync(body, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(created);
    }

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> UpdateRejectionReason(
        Guid id, [FromBody] RejectionReasonOptionInput body, CancellationToken cancellationToken)
        => RunReviewActionAsync(actor => rejectionReasonService.UpdateAsync(id, body, actor, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteRejectionReason(Guid id, CancellationToken cancellationToken)
        => RunReviewActionAsync(actor => rejectionReasonService.DeleteAsync(id, actor, cancellationToken));
}
