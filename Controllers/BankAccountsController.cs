using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/bank-accounts")]
public class BankAccountsController(IBankAccountService bankAccountService) : PmisAuthorizedControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> BankAccounts(CancellationToken cancellationToken)
        => Ok(await bankAccountService.GetAllAsync(cancellationToken));

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBankAccount(
        [FromBody] BankAccountInput body, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var (created, error) = await bankAccountService.CreateAsync(body, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(created);
    }

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBankAccount(
        Guid id, [FromBody] BankAccountInput body, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var error = await bankAccountService.UpdateAsync(id, body, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBankAccount(Guid id, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null || actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ForbiddenActionError });
        }

        var error = await bankAccountService.DeleteAsync(id, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }
}
