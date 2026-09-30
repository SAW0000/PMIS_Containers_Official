using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.Interfaces;

namespace PMIS.Controllers;

[Route("app/notifications")]
public class NotificationsController(INotificationQueryService notificationQueryService) : PmisAuthorizedControllerBase
{
    [HttpPost("{notificationId}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(string notificationId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        await notificationQueryService.MarkReadAsync(notificationId, userId, cancellationToken);
        return Ok();
    }

    [HttpPost("mark-all-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        await notificationQueryService.MarkAllReadAsync(userId, cancellationToken);
        return Ok();
    }

    [HttpGet("")]
    public async Task<IActionResult> Notifications(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var result = await notificationQueryService.GetPagedForUserAsync(
            userId, page <= 0 ? 1 : page, pageSize <= 0 ? 10 : pageSize, cancellationToken);
        return new JsonResult(result, JsonOptions);
    }

    [HttpGet("unread")]
    public async Task<IActionResult> UnreadNotifications(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var notifications = await notificationQueryService.GetUnreadForUserAsync(userId, cancellationToken);
        return new JsonResult(notifications, JsonOptions);
    }
}
