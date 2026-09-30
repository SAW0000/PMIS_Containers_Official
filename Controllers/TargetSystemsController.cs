using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/target-systems")]
public class TargetSystemsController(ITargetSystemService targetSystemService) : PmisAuthorizedControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> TargetSystems(CancellationToken cancellationToken)
        => Ok(await targetSystemService.GetAllForAdminAsync(cancellationToken));

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTargetSystem(
        [FromBody] TargetSystemInput body, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var (created, error) = await targetSystemService.CreateAsync(body, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(created);
    }

    [HttpPut("{id:guid}/active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTargetSystemActive(
        Guid id, [FromBody] SetActiveInput body, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var error = await targetSystemService.SetActiveAsync(id, body.IsActive, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTargetSystem(Guid id, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var error = await targetSystemService.DeleteAsync(id, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }
}
