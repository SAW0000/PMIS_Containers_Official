using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/users")]
public class UserPhotosController(IUserPhotoReadService userPhotoReadService) : PmisAuthorizedControllerBase
{
    [HttpGet("{userId:guid}/photo")]
    public async Task<IActionResult> DownloadUserPhoto(Guid userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var requesterId)
            || !Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var requesterRole))
        {
            return Challenge();
        }

        var photo = await userPhotoReadService.OpenPhotoAsync(userId, requesterId, requesterRole, cancellationToken);
        if (photo is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";

        Response.Headers.CacheControl = "private, max-age=300";

        return File(photo.Content, photo.ContentType);
    }
}
