using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app/requests")]
public class RequestAttachmentsController(IRequestAttachmentService requestAttachmentService) : PmisAuthorizedControllerBase
{
    [HttpPost("{requestId:guid}/attachments/{attachmentType}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(
        Guid requestId, AttachmentType attachmentType, IFormFile file, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        if (file is not { Length: > 0 })
        {
            return BadRequest(new { message = "الملف مطلوب" });
        }

        await using var stream = file.OpenReadStream();
        var uploadedFile = new UploadedFile(stream, file.FileName, file.ContentType, file.Length);
        var (attachment, error) = await requestAttachmentService.UploadAsync(
            requestId, attachmentType, uploadedFile, actor, cancellationToken);

        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(attachment);
    }

    [HttpGet("{requestId:guid}/attachments")]
    public async Task<IActionResult> ListAttachments(Guid requestId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var userRole))
        {
            return Challenge();
        }

        var attachments = await requestAttachmentService.ListForRequestAsync(requestId, userId, userRole, cancellationToken);
        return new JsonResult(attachments, JsonOptions);
    }

    [HttpGet("attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        var (download, error) = await requestAttachmentService.OpenForDownloadAsync(
            attachmentId, actor.UserId, actor.Role, actor, cancellationToken);

        if (error is not null)
        {
            return StatusCode(StatusCodes.Status423Locked, new { message = error });
        }

        if (download is null)
        {
            return NotFound();
        }

        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpDelete("attachments/{attachmentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null)
        {
            return Challenge();
        }

        var error = await requestAttachmentService.DeleteAsync(attachmentId, actor, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }
}
