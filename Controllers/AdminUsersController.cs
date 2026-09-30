using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/admin/users")]
public class AdminUsersController(
    IAdminUserService adminUserService,
    IAdminUserQueryService adminUserQueryService) : PmisAuthorizedControllerBase
{
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([FromBody] AdminUserCreateDto dto, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();
        if (actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بإضافة مستخدمين" });
        }

        var error = await adminUserService.CreateAsync(dto, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUserUpdateDto dto, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();
        if (actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بتعديل بيانات المستخدمين" });
        }

        var error = await adminUserService.UpdateAsync(id, dto, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();
        if (actor.Role != UserRole.SystemAdministrator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "غير مصرح لك بحذف المستخدمين" });
        }

        var error = await adminUserService.DeleteAsync(id, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpGet("page")]
    public async Task<IActionResult> AdminUsersPage([FromQuery] AdminUserListQuery query, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var page = await adminUserQueryService.GetUsersPageAsync(userRole, query, cancellationToken);
        return new JsonResult(page, JsonOptions);
    }
}
