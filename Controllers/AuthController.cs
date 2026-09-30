using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Controllers;

[Route("")]
public partial class AuthController(
    IUnitOfWork unitOfWork,
    IUserPhotoUploadService photoUploadService,
    IAntiforgery antiforgery,
    INafathSsoCoordinator nafathSsoCoordinator,
    IOtpService otpService,
    ISystemSettingsService systemSettingsService,
    IWebHostEnvironment environment) : Controller
{
    private NafathSettings nafathSettings => nafathSsoCoordinator.Settings;
    private const string NafathTokenCookieName = "NafathSsoToken";
    private const string PendingProfileCookieName = "NafathPendingProfile";
    private const string PendingProfileExpiredMessage = "انتهت صلاحية الجلسة، يرجى الدخول عبر نفاذ مرة أخرى";
    private const string ForceReauthCookieName = "NafathReauthRequired";
    private static readonly TimeSpan NafathStateLifetime = TimeSpan.FromMinutes(10);

    private bool CookieShouldBeSecure => !environment.IsDevelopment() || Request.IsHttps;


    [HttpGet("")]
    [HttpGet("login")]
    public async Task<IActionResult> Login(
        string? nafathError = null,
        bool completeProfile = false,
        bool loggedOut = false,
        bool confirmReauth = false,
        UserRole? role = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        ViewBag.AntiForgeryToken = tokens.RequestToken;
        ViewBag.NafathError = nafathError;
        ViewBag.LoggedOut = loggedOut;

        var settings = await systemSettingsService.GetAsync(cancellationToken);
        ViewBag.OtpResendCooldownSeconds = settings.OtpResendCooldownSeconds;


        PreventCachingOfAuthenticatedPages();

        if (confirmReauth && role is { } pickedRole && Enum.IsDefined(pickedRole))
        {
            ViewBag.ConfirmReauthRole = pickedRole.ToString();
        }

        if (completeProfile)
        {
            var pendingUser = await ResolvePendingProfileUserAsync(cancellationToken);
            if (pendingUser is null)
            {
                return RedirectToAction(nameof(Login));
            }

            ViewBag.PendingNationalId = pendingUser.NationalId;
            ViewBag.PendingFullName = pendingUser.FullName;

            ViewBag.PendingArabicFirstName = pendingUser.ArabicFirstName;
            ViewBag.PendingArabicFatherName = pendingUser.ArabicFatherName;
            ViewBag.PendingArabicGrandFatherName = pendingUser.ArabicGrandFatherName;
            ViewBag.PendingArabicFamilyName = pendingUser.ArabicFamilyName;
            ViewBag.PendingArabicNationality = pendingUser.ArabicNationality;
            ViewBag.PendingGender = GenderFormat.ToArabicLabel(pendingUser.Gender);
        }

        return View();
    }

    private async Task<User?> ResolvePendingProfileUserAsync(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[PendingProfileCookieName];
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var userId = await nafathSsoCoordinator.ConsumePendingProfileAsync(token, cancellationToken);
        if (userId is null)
        {
            return null;
        }

        var user = await unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || user.DeletedAccount || !string.IsNullOrEmpty(user.Email))
        {
            return null;
        }

        await IssuePendingProfileCookieAsync(user.Id, cancellationToken);
        return user;
    }

    private async Task IssuePendingProfileCookieAsync(Guid userId, CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N");
        await nafathSsoCoordinator.StorePendingProfileAsync(token, userId, NafathStateLifetime, cancellationToken);
        Response.Cookies.Append(PendingProfileCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = CookieShouldBeSecure,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(NafathStateLifetime)
        });
    }

    private static string RegistrationOtpContext(Guid userId) => $"register:{userId}";

    [HttpPost("register/send-verification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRegistrationVerification(
        [FromBody] SendRegistrationOtpRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolvePendingProfileUserAsync(cancellationToken);
        if (user is null)
        {
            return BadRequest(new { message = PendingProfileExpiredMessage });
        }

        var validationError = await ValidateEmailAndMobile(request.Email, request.Mobile, user.Id, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        return await DeliverBothCodesAsync(
            RegistrationOtpContext(user.Id), request.Email.Trim(), request.Mobile.Trim(), cancellationToken);
    }

    [HttpPost("register/resend-verification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendRegistrationVerification(
        [FromBody] SendRegistrationOtpRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolvePendingProfileUserAsync(cancellationToken);
        if (user is null)
        {
            return BadRequest(new { message = PendingProfileExpiredMessage });
        }

        var validationError = await ValidateEmailAndMobile(request.Email, request.Mobile, user.Id, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        return await DeliverBothCodesAsync(
            RegistrationOtpContext(user.Id), request.Email.Trim(), request.Mobile.Trim(), cancellationToken, isResend: true);
    }

    [HttpPost("register/complete-profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(
        [FromForm] RegisterRequest request, CancellationToken cancellationToken)
    {
        var token = Request.Cookies[PendingProfileCookieName];
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = PendingProfileExpiredMessage });
        }

        var userId = await nafathSsoCoordinator.ConsumePendingProfileAsync(token, cancellationToken);
        Response.Cookies.Delete(PendingProfileCookieName);
        if (userId is null)
        {
            return BadRequest(new { message = PendingProfileExpiredMessage });
        }

        var user = await unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || user.DeletedAccount || !string.IsNullOrEmpty(user.Email))
        {
            return BadRequest(new { message = PendingProfileExpiredMessage });
        }

        var validationError = await ValidateEmailAndMobile(request.Email, request.Mobile, user.Id, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var email = request.Email.Trim();

        var verifyOutcome = await VerifyBothCodesAsync(
            RegistrationOtpContext(user.Id), request.EmailCode, request.MobileCode, cancellationToken);
        if (verifyOutcome is not null)
        {
            return verifyOutcome;
        }

        ScannedPhoto? scannedPhoto = null;
        if (request.Photo is { Length: > 0 } photo)
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
            return await FinishProfileAsync(user, email, request, scannedPhoto, cancellationToken);
        }
    }

    private async Task<IActionResult?> ValidateEmailAndMobile(
        string? email, string? mobile, Guid userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(mobile))
        {
            return BadRequest(new { message = "جميع الحقول (باستثناء الصورة الشخصية) مطلوبة" });
        }

        if (!EmailFormat.IsValid(email))
        {
            return BadRequest(new { message = "صيغة البريد الإلكتروني غير صحيحة" });
        }

        if (!MobileFormat.IsValid(mobile))
        {
            return BadRequest(new { message = "رقم الجوال غير صحيح — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام" });
        }

        var trimmedEmail = email.Trim();
        var emailTaken = await unitOfWork.Users.FindAsync(u => u.Id != userId && u.Email == trimmedEmail, cancellationToken);
        if (emailTaken.Count > 0)
        {
            return Conflict(new { message = "هذا البريد الإلكتروني مسجل بالفعل" });
        }

        return null;
    }

    private async Task<IActionResult> DeliverBothCodesAsync(
        string contextId, string email, string mobile, CancellationToken cancellationToken, bool isResend = false)
    {
        var emailResult = isResend
            ? await otpService.ResendAsync(contextId, OtpChannel.Email, email, cancellationToken)
            : await otpService.SendAsync(contextId, OtpChannel.Email, email, cancellationToken);
        if (emailResult != OtpSendResult.Sent)
        {
            return MapOtpSendFailure(emailResult);
        }

        var mobileResult = isResend
            ? await otpService.ResendAsync(contextId, OtpChannel.Mobile, mobile, cancellationToken)
            : await otpService.SendAsync(contextId, OtpChannel.Mobile, mobile, cancellationToken);
        if (mobileResult != OtpSendResult.Sent)
        {
            return MapOtpSendFailure(mobileResult);
        }

        return Ok();
    }

    private ObjectResult MapOtpSendFailure(OtpSendResult result)
    {
        var message = OtpResultMessages.ForSendFailure(result) ?? OtpResultMessages.InfrastructureProblem;
        var statusCode = message == OtpResultMessages.InfrastructureProblem
            ? StatusCodes.Status502BadGateway
            : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, new { message });
    }

    private async Task<IActionResult?> VerifyBothCodesAsync(
        string contextId, string emailCode, string mobileCode, CancellationToken cancellationToken)
    {
        var emailOutcome = await otpService.VerifyAsync(contextId, OtpChannel.Email, emailCode, cancellationToken);
        if (emailOutcome != OtpVerifyResult.Verified)
        {
            return MapOtpVerifyFailure(emailOutcome);
        }

        var mobileOutcome = await otpService.VerifyAsync(contextId, OtpChannel.Mobile, mobileCode, cancellationToken);
        if (mobileOutcome != OtpVerifyResult.Verified)
        {
            return MapOtpVerifyFailure(mobileOutcome);
        }

        return null;
    }

    private ObjectResult MapOtpVerifyFailure(OtpVerifyResult result)
    {
        var message = OtpResultMessages.ForVerifyFailure(result) ?? OtpResultMessages.InfrastructureProblem;
        var statusCode = message == OtpResultMessages.InfrastructureProblem
            ? StatusCodes.Status502BadGateway
            : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, new { message });
    }

    private async Task<IActionResult> FinishProfileAsync(
        User user, string email, RegisterRequest request, ScannedPhoto? scannedPhoto, CancellationToken cancellationToken)
    {
        user.Email = email;
        user.Mobile = request.Mobile.Trim();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (scannedPhoto is not null)
        {
            var supersededPhotoRef = await photoUploadService.AttachToUserAsync(
                scannedPhoto, user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(supersededPhotoRef))
            {
                await photoUploadService.DeleteSupersededAsync(supersededPhotoRef, user.Id, cancellationToken);
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, UserRole.ContractorConsultant.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { redirectUrl = "/app" });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var ssoToken = Request.Cookies[NafathTokenCookieName];
        if (!string.IsNullOrEmpty(ssoToken))
        {
            await nafathSsoCoordinator.ConsumeRoleForTokenAsync(ssoToken, HttpContext.RequestAborted);
        }

        var pendingToken = Request.Cookies[PendingProfileCookieName];
        if (!string.IsNullOrEmpty(pendingToken))
        {
            await nafathSsoCoordinator.ConsumePendingProfileAsync(pendingToken, HttpContext.RequestAborted);
        }

        Response.Cookies.Delete(NafathTokenCookieName);
        Response.Cookies.Delete(PendingProfileCookieName);

        MarkReauthRequired();
        PreventCachingOfAuthenticatedPages();

        var redirectUrl = string.IsNullOrEmpty(nafathSettings.IamLogoutUrl)
            ? "/login?loggedOut=true"
            : nafathSettings.IamLogoutUrl;

        return Ok(new { redirectUrl });
    }

    private void MarkReauthRequired()
    {
        Response.Cookies.Append(ForceReauthCookieName, "1", new CookieOptions
        {
            HttpOnly = true,
            Secure = CookieShouldBeSecure,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(12)
        });
    }

    private void PreventCachingOfAuthenticatedPages()
    {
        Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
    }

    [HttpGet("login/nafath")]
    [AllowAnonymous]
    public async Task<IActionResult> NafathLogin(
        UserRole role, bool confirmed = false, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(role))
        {
            return BadRequest(new { message = "دور غير صالح" });
        }

        if (!confirmed && Request.Cookies.ContainsKey(ForceReauthCookieName))
        {
            return RedirectToAction(nameof(Login), new { confirmReauth = true, role });
        }

        Response.Cookies.Delete(ForceReauthCookieName);

        var token = await nafathSsoCoordinator.GetOneTimeTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(Login), new { nafathError = "connect" });
        }

        await nafathSsoCoordinator.StoreRoleForTokenAsync(token, role, NafathStateLifetime, cancellationToken);

        Response.Cookies.Append(NafathTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = CookieShouldBeSecure,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(NafathStateLifetime)
        });

        if (!string.IsNullOrEmpty(nafathSettings.BypassNationalId))
        {
            return RedirectToAction(nameof(NafathCallback));
        }

        return Redirect($"{nafathSettings.IamLoginUrl}?token={Uri.EscapeDataString(token)}");
    }

    [HttpGet("login/nafath/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> NafathCallback(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[NafathTokenCookieName];
        Response.Cookies.Delete(NafathTokenCookieName);
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(Login), new { nafathError = "expired" });
        }

        var role = await nafathSsoCoordinator.ConsumeRoleForTokenAsync(token, cancellationToken);
        if (role is null)
        {
            return RedirectToAction(nameof(Login), new { nafathError = "expired" });
        }

        var userClaim = await nafathSsoCoordinator.GetUserClaimAsync(token, cancellationToken);
        if (userClaim is null || string.IsNullOrEmpty(userClaim.NameIdentifier))
        {
            return RedirectToAction(nameof(Login), new { nafathError = "connect" });
        }

        var nationalId = userClaim.NameIdentifier;
        var matches = await unitOfWork.Users.FindAsync(u => u.NationalId == nationalId, cancellationToken);
        var user = matches.Count > 0 ? matches[0] : null;

        if (user is null || user.DeletedAccount)
        {
            if (role is UserRole.P3OOfficer or UserRole.SystemAdministrator)
            {
                return RedirectToAction(nameof(Login), new { nafathError = "not_registered" });
            }

            return await CreateOrReviveContractorConsultantAsync(user, nationalId, userClaim, cancellationToken);
        }

        var assignedRoles = await unitOfWork.UserRoleAssignments.FindAsync(
            ra => ra.UserId == user.Id, cancellationToken);
        if (!assignedRoles.Any(ra => ra.Role == role))
        {
            return RedirectToAction(nameof(Login), new { nafathError = "role_mismatch" });
        }

        if (!user.IsActive)
        {
            return RedirectToAction(nameof(Login), new { nafathError = "not_registered" });
        }

        if (role == UserRole.ContractorConsultant && string.IsNullOrEmpty(user.Email))
        {
            return await RedirectToProfileCompletionAsync(user, cancellationToken);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role.Value.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Redirect("/app");
    }

    private async Task<IActionResult> CreateOrReviveContractorConsultantAsync(
        User? user, string nationalId, NafathUserClaim userClaim, CancellationToken cancellationToken)
    {
        if (user is { DeletedAccount: true } revived)
        {
            revived.DeletedAccount = false;
            revived.FullName = userClaim.ArabicName;
            revived.Email = null;
            revived.Mobile = null;
            revived.IsActive = true;
            ApplyNafathIdentity(revived, userClaim);
            user = revived;

            var priorRoles = await unitOfWork.UserRoleAssignments.FindAsync(
                ra => ra.UserId == user.Id, cancellationToken);
            foreach (var priorRole in priorRoles)
            {
                unitOfWork.UserRoleAssignments.Remove(priorRole);
            }
        }
        else
        {
            user = new User
            {
                FullName = userClaim.ArabicName,
                NationalId = nationalId,
                IsActive = true
            };
            ApplyNafathIdentity(user, userClaim);
            await unitOfWork.Users.AddAsync(user, cancellationToken);
        }

        user.RoleAssignments.Add(new UserRoleAssignment { Role = UserRole.ContractorConsultant });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await RedirectToProfileCompletionAsync(user, cancellationToken);
    }

    private static void ApplyNafathIdentity(User user, NafathUserClaim claim)
    {
        user.ArabicFirstName = claim.ArabicFirstName;
        user.ArabicFatherName = claim.ArabicFatherName;
        user.ArabicGrandFatherName = claim.ArabicGrandFatherName;
        user.ArabicFamilyName = claim.ArabicFamilyName;

        var arabicNationality = NafathArabicNationalityFormat.ForStorage(claim.ArabicNationality);
        user.ArabicNationality = string.IsNullOrWhiteSpace(arabicNationality) ? claim.Nationality : arabicNationality;

        user.Gender = GenderFormat.Parse(claim.Gender);
    }

    private async Task<IActionResult> RedirectToProfileCompletionAsync(User user, CancellationToken cancellationToken)
    {
        await IssuePendingProfileCookieAsync(user.Id, cancellationToken);
        return RedirectToAction(nameof(Login), new { completeProfile = true });
    }
}
