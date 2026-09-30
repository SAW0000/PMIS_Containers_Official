using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;

namespace PMIS.Controllers;

[Route("app/account-settings")]
public class AccountSettingsController(
    INotificationPreferenceService notificationPreferenceService,
    IAccountSettingsService accountSettingsService,
    IUserPhotoUploadService photoUploadService) : PmisAuthorizedControllerBase
{
    [HttpGet("notification-preferences")]
    public async Task<IActionResult> NotificationPreferences(CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();

        var preferences = await notificationPreferenceService.GetForUserAsync(
            actor.UserId, actor.Role, cancellationToken);
        return new JsonResult(preferences, JsonOptions);
    }

    [HttpPost("notification-preferences")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] NotificationPreferenceUpdateDto dto, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();

        var error = await notificationPreferenceService.UpdateAsync(
            actor.UserId, actor.Role, dto.Preferences, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok();
    }

    [HttpPost("send-verification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAccountContactVerification(
        [FromBody] AccountContactVerificationRequestDto dto, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();

        var (result, error) = await accountSettingsService.SendContactVerificationAsync(
            actor.UserId, dto, isResend: false, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return new JsonResult(result, JsonOptions);
    }

    [HttpPost("resend-verification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendAccountContactVerification(
        [FromBody] AccountContactVerificationRequestDto dto, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();

        var (result, error) = await accountSettingsService.SendContactVerificationAsync(
            actor.UserId, dto, isResend: true, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return new JsonResult(result, JsonOptions);
    }

    [HttpPost("info")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccountInfo(
        [FromForm] AccountInfoUpdateDto dto, IFormFile? photo, CancellationToken cancellationToken)
    {
        var actor = TryGetActor();
        if (actor is null) return Challenge();

        ScannedPhoto? scannedPhoto = null;
        if (photo is { Length: > 0 })
        {
            string? photoError;
            await using var content = photo.OpenReadStream();
            (scannedPhoto, photoError) = await photoUploadService.ValidateAndScanAsync(
                content, photo.FileName, photo.ContentType, cancellationToken);

            if (photoError is not null)
            {
                return BadRequest(new { message = photoError });
            }
        }

        using (scannedPhoto)
        {
            var (result, error) = await accountSettingsService.UpdateAccountInfoAsync(
                actor.UserId, dto, scannedPhoto, actor.IpAddress, cancellationToken);

            if (error is not null)
            {
                return BadRequest(new { message = error });
            }

            if (result!.IdentityRefreshed)
            {
                await RefreshIdentityCookieAsync(actor, actor.FullName, result.Email);
            }

            return new JsonResult(result, JsonOptions);
        }
    }

    private Task RefreshIdentityCookieAsync(RequestActor actor, string name, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actor.UserId.ToString()),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, actor.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
