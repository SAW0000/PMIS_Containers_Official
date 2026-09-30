using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;

namespace PMIS.Controllers;

[Route("app")]
public class AppShellController(
    IAntiforgery antiforgery,
    IRequestQueryService requestQueryService,
    IRejectionReasonService rejectionReasonService,
    IBankAccountService bankAccountService,
    ISystemSettingsService systemSettingsService,
    ITargetSystemService targetSystemService,
    IUserProfileQueryService userProfileQueryService) : PmisAuthorizedControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        ViewBag.CurrentRole = role;

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Challenge();
        }

        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        ViewBag.AntiForgeryToken = tokens.RequestToken;

        var requests = await requestQueryService.GetVisibleRequestsAsync(userId, userRole, includeDetails: false, cancellationToken: cancellationToken);
        ViewBag.RequestsJson = JsonSerializer.Serialize(requests, JsonOptions);

        var targetSystems = await requestQueryService.GetActiveSystemsAsync(cancellationToken);
        ViewBag.TargetSystemsJson = JsonSerializer.Serialize(targetSystems, JsonOptions);

        var rejectionReasons = await rejectionReasonService.GetAllAsync(cancellationToken);
        ViewBag.RejectionReasonsJson = JsonSerializer.Serialize(rejectionReasons, JsonOptions);

        var bankAccounts = await bankAccountService.GetAllAsync(cancellationToken);
        ViewBag.BankAccountsJson = JsonSerializer.Serialize(bankAccounts, JsonOptions);


        var settings = userRole == UserRole.SystemAdministrator
            ? await systemSettingsService.GetAsync(cancellationToken)
            : null;
        ViewBag.SystemSettingsJson = JsonSerializer.Serialize(settings, JsonOptions);

        var uploadPolicySource = settings ?? await systemSettingsService.GetAsync(cancellationToken);
        ViewBag.MaxUploadSizeMb = uploadPolicySource.MaxUploadSizeMb;
        ViewBag.AllowedAttachmentExtensionsJson =
            JsonSerializer.Serialize(uploadPolicySource.AllowedAttachmentExtensions, JsonOptions);

        ViewBag.ResponseSlaBusinessDays = uploadPolicySource.ResponseSlaBusinessDays;

        ViewBag.AllowedProfilePhotoExtensionsJson =
            JsonSerializer.Serialize(uploadPolicySource.AllowedProfilePhotoExtensions, JsonOptions);

        ViewBag.TermsAndConditionsTextJson = JsonSerializer.Serialize(uploadPolicySource.TermsAndConditionsText, JsonOptions);
        ViewBag.TermsAgreementLabelJson = JsonSerializer.Serialize(uploadPolicySource.TermsAgreementLabel, JsonOptions);

        ViewBag.OtpResendCooldownSeconds = uploadPolicySource.OtpResendCooldownSeconds;

        var adminTargetSystems = userRole == UserRole.SystemAdministrator
            ? await targetSystemService.GetAllForAdminAsync(cancellationToken)
            : null;
        ViewBag.AdminTargetSystemsJson = JsonSerializer.Serialize(adminTargetSystems, JsonOptions);



        var profile = await userProfileQueryService.GetOwnProfileAsync(userId, userRole, cancellationToken);
        if (profile is null)
        {
            return Challenge();
        }
        ViewBag.CurrentUserJson = JsonSerializer.Serialize(profile, JsonOptions);

        return View("~/Views/App/Index.cshtml");
    }
}
