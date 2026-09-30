using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Controllers;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Tests;

public class AuthControllerNafathTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<UserRoleAssignment>> _userRoleAssignments = new();
    private readonly Mock<INafathSsoClient> _nafathSsoClient = new();
    private readonly Mock<IAuthenticationService> _authenticationService = new();
    private readonly FakeNafathLoginStateStore _nafathLoginStateStore = new();
    private readonly FakeNafathPendingProfileStore _nafathPendingProfileStore = new();
    private readonly Mock<ILoginLockoutService> _loginLockoutService = new();
    private readonly Mock<IAntiforgery> _antiforgery = new();
    private readonly Mock<IOtpService> _otpService = new();
    private readonly Mock<ISystemSettingsService> _systemSettingsService = new();

    public AuthControllerNafathTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(u => u.UserRoleAssignments).Returns(_userRoleAssignments.Object);
        _authenticationService
            .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        _loginLockoutService.Setup(l => l.IsLockedOutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _antiforgery.Setup(a => a.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "__RequestVerificationToken", "X-CSRF-TOKEN"));
        _systemSettingsService.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemSettingsDto
            {
                LicenseValidityMonths = 12,
                ResponseSlaBusinessDays = 3,
                MaxActiveRequestsPerUser = 5,
                MaxUploadSizeMb = 20,
                MaxLoginAttempts = 10,
                LoginLockoutMinutes = 2,
                OtpExpiryMinutes = 5,
                OtpResendCooldownSeconds = 45,
                OtpMaxResendCount = 5,
                AuditLogRetentionDays = 0,
                NotificationRetentionDays = 0,
                AuditLogMaxCount = 0,
                NotificationMaxCount = 0
            });
    }

    private AuthController CreateController(NafathSettings? nafathSettings = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_authenticationService.Object);
        services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        var nafathOptions = Options.Create(nafathSettings ?? new NafathSettings
        {
            IamSsoUrl = "https://sso.example.test/",
            GetTokenUrl = "Account/GeneraterToken?appkey=",
            UserClaimsUrl = "Account/GetUserClaim?token=",
            AppKey = "test-app-key",
            IamLoginUrl = "https://sso.example.test/Account/IamLogin"
        });

        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor
        {
            ActionName = nameof(AuthController.Login),
            ControllerName = "Auth"
        });

        var nafathSsoCoordinator = new NafathSsoCoordinator(
            _nafathSsoClient.Object, _nafathLoginStateStore, _nafathPendingProfileStore, nafathOptions);

        return new AuthController(
            _unitOfWork.Object,
            Mock.Of<IUserPhotoUploadService>(),
            _antiforgery.Object,
            nafathSsoCoordinator,
            _otpService.Object,
            _systemSettingsService.Object,
            Mock.Of<IWebHostEnvironment>())
        {
            ControllerContext = new ControllerContext(actionContext)
        };
    }

    private void SetupUsersFind(List<User> users)
    {
        _users.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<User, bool>> predicate, CancellationToken _) =>
                users.Where(predicate.Compile()).ToList());
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => users.FirstOrDefault(u => u.Id == id));

        SetupRoleAssignments(users
            .SelectMany(u => u.RoleAssignments.Select(ra => new UserRoleAssignment { UserId = u.Id, Role = ra.Role }))
            .ToList());
    }

    private void SetupRoleAssignments(List<UserRoleAssignment> assignments)
    {
        _userRoleAssignments.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserRoleAssignment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<UserRoleAssignment, bool>> predicate, CancellationToken _) =>
                assignments.Where(predicate.Compile()).ToList());
    }

    private static JsonElement JsonString(string value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task NafathLogin_ReturnsBadRequest_ForUndefinedRole()
    {
        var controller = CreateController();

        var result = await controller.NafathLogin((UserRole)999, cancellationToken: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task NafathLogin_RedirectsToLoginWithConnectError_WhenTokenRequestFails()
    {
        _nafathSsoClient.Setup(c => c.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var controller = CreateController();

        var result = await controller.NafathLogin(UserRole.ContractorConsultant, cancellationToken: CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("connect", redirect.RouteValues!["nafathError"]);
    }

    [Fact]
    public async Task NafathLogin_RedirectsToAmanaIamLoginUrl_WhenTokenRequestSucceeds()
    {
        _nafathSsoClient.Setup(c => c.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("sso-token-123");
        var controller = CreateController();

        var result = await controller.NafathLogin(UserRole.ContractorConsultant, cancellationToken: CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("https://sso.example.test/Account/IamLogin", redirect.Url);
    }

    [Fact]
    public async Task NafathCallback_RedirectsWithExpiredError_WhenNoTokenCookiePresent()
    {
        var controller = CreateController();

        var result = await controller.NafathCallback(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("expired", redirect.RouteValues!["nafathError"]);
    }

    [Fact]
    public async Task NafathCallback_RejectsUnregisteredP3OOfficer_WithNotRegisteredError()
    {
        SetupUsersFind([]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim { NameIdentifier = "1112223334", ArabicName = "فهد سالم" });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.P3OOfficer);

        var result = await controller.NafathCallback(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("not_registered", redirect.RouteValues!["nafathError"]);
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NafathCallback_CreatesPendingContractorConsultant_WhenNoExistingUserMatches()
    {
        SetupUsersFind([]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim { NameIdentifier = "1112223334", ArabicName = "فهد سالم" });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.ContractorConsultant);

        var result = await controller.NafathCallback(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.Login), redirect.ActionName);
        Assert.True((bool)redirect.RouteValues!["completeProfile"]!);
        _users.Verify(r => r.AddAsync(
            It.Is<User>(u => u.NationalId == "1112223334"
                && u.RoleAssignments.Any(ra => ra.Role == UserRole.ContractorConsultant)),
            It.IsAny<CancellationToken>()), Times.Once);
        _authenticationService.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()),
            Times.Never);
        Assert.Single(_nafathPendingProfileStore.Seeded);
    }

    [Fact]
    public async Task NafathCallback_PopulatesNafathIdentityFields_ForNewAccount()
    {
        SetupUsersFind([]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim
            {
                NameIdentifier = "1112223334",
                ArabicName = "فهد سالم الرحيلي",
                ArabicFirstName = "فهد",
                ArabicFatherName = "سالم",
                ArabicGrandFatherName = "راجح",
                ArabicFamilyName = "الرحيلي",
                Nationality = "Saudi Arabia",
                ArabicNationality = JsonString("سعودي"),
                DobGregorian = "Sat May 08 03:00:00 ",
                Gender = "M"
            });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.ContractorConsultant);

        await controller.NafathCallback(CancellationToken.None);

        _users.Verify(r => r.AddAsync(
            It.Is<User>(u => u.ArabicFirstName == "فهد" && u.ArabicFatherName == "سالم"
                && u.ArabicGrandFatherName == "راجح" && u.ArabicFamilyName == "الرحيلي"
                && u.ArabicNationality == "سعودي" && u.Gender == Gender.Male),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NafathCallback_PopulatesNafathIdentityFields_ForRevivedAccount()
    {
        var deletedUser = new User
        {
            Id = Guid.NewGuid(), NationalId = "1112223334", FullName = "فهد سالم",
            DeletedAccount = true, IsActive = false
        };
        SetupUsersFind([deletedUser]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim
            {
                NameIdentifier = "1112223334",
                ArabicName = "فهد سالم الرحيلي",
                ArabicFirstName = "فهد",
                ArabicFatherName = "سالم",
                ArabicGrandFatherName = "راجح",
                ArabicFamilyName = "الرحيلي",
                Nationality = "Saudi Arabia",
                ArabicNationality = JsonString("سعودي"),
                DobGregorian = "Sat May 08 03:00:00 ",
                Gender = "M"
            });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.ContractorConsultant);

        await controller.NafathCallback(CancellationToken.None);

        Assert.Equal("فهد", deletedUser.ArabicFirstName);
        Assert.Equal("سالم", deletedUser.ArabicFatherName);
        Assert.Equal("راجح", deletedUser.ArabicGrandFatherName);
        Assert.Equal("الرحيلي", deletedUser.ArabicFamilyName);
        Assert.Equal("سعودي", deletedUser.ArabicNationality);
        Assert.Equal(Gender.Male, deletedUser.Gender);
    }

    [Fact]
    public async Task NafathCallback_FallsBackToEnglishNationality_WhenArabicNationalityIsUnparseable()
    {
        SetupUsersFind([]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim
            {
                NameIdentifier = "1112223334",
                ArabicName = "فهد سالم",
                Nationality = "Saudi Arabia"
            });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.ContractorConsultant);

        await controller.NafathCallback(CancellationToken.None);

        _users.Verify(r => r.AddAsync(
            It.Is<User>(u => u.ArabicNationality == "Saudi Arabia"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NafathCallback_DoesNotTouchNafathIdentityFields_ForExistingReturningUser()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(), NationalId = "1112223334",
            RoleAssignments = [new UserRoleAssignment { Role = UserRole.SystemAdministrator }],
            FullName = "فهد سالم", IsActive = true,
            ArabicFirstName = "قديم"
        };
        SetupUsersFind([existingUser]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim
            {
                NameIdentifier = "1112223334", ArabicName = "فهد سالم", ArabicFirstName = "جديد"
            });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.SystemAdministrator);

        await controller.NafathCallback(CancellationToken.None);

        Assert.Equal("قديم", existingUser.ArabicFirstName);
    }

    [Fact]
    public async Task NafathCallback_ResendsToProfileCompletion_WhenContractorConsultantNeverFinishedIt()
    {
        var pendingUser = new User
        {
            Id = Guid.NewGuid(), NationalId = "1112223334",
            RoleAssignments = [new UserRoleAssignment { Role = UserRole.ContractorConsultant }],
            FullName = "فهد سالم", IsActive = true
        };
        SetupUsersFind([pendingUser]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim { NameIdentifier = "1112223334", ArabicName = "فهد سالم" });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.ContractorConsultant);

        var result = await controller.NafathCallback(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.Login), redirect.ActionName);
        Assert.True((bool)redirect.RouteValues!["completeProfile"]!);
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _authenticationService.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task NafathCallback_SignsInExistingUser_WhenNationalIdMatches()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(), NationalId = "1112223334",
            RoleAssignments = [new UserRoleAssignment { Role = UserRole.SystemAdministrator }],
            FullName = "فهد سالم", IsActive = true
        };
        SetupUsersFind([existingUser]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim { NameIdentifier = "1112223334", ArabicName = "فهد سالم" });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.SystemAdministrator);

        var result = await controller.NafathCallback(CancellationToken.None);

        Assert.IsType<RedirectResult>(result);
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _authenticationService.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Fact]
    public async Task NafathCallback_SignsInUserWithMultipleAssignedRoles_WhenPickedRoleIsAssigned()
    {
        var dualRoleUser = new User { Id = Guid.NewGuid(), NationalId = "1112223334", FullName = "فهد سالم", IsActive = true };
        _users.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dualRoleUser]);
        SetupRoleAssignments([
            new UserRoleAssignment { UserId = dualRoleUser.Id, Role = UserRole.SystemAdministrator },
            new UserRoleAssignment { UserId = dualRoleUser.Id, Role = UserRole.P3OOfficer }
        ]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim { NameIdentifier = "1112223334", ArabicName = "فهد سالم" });

        ClaimsPrincipal? signedInPrincipal = null;
        _authenticationService
            .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, principal, _) => signedInPrincipal = principal)
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.P3OOfficer);

        var result = await controller.NafathCallback(CancellationToken.None);

        Assert.IsType<RedirectResult>(result);
        Assert.NotNull(signedInPrincipal);
        Assert.Equal(nameof(UserRole.P3OOfficer), signedInPrincipal!.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public async Task NafathCallback_RejectsRoleNeverAssignedToAccount_WithRoleMismatchError()
    {
        var contractorOnlyUser = new User
        {
            Id = Guid.NewGuid(), NationalId = "1112223334",
            RoleAssignments = [new UserRoleAssignment { Role = UserRole.ContractorConsultant }],
            FullName = "فهد سالم", IsActive = true, Email = "fahad@example.test"
        };
        SetupUsersFind([contractorOnlyUser]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim { NameIdentifier = "1112223334", ArabicName = "فهد سالم" });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.SystemAdministrator);

        var result = await controller.NafathCallback(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("role_mismatch", redirect.RouteValues!["nafathError"]);
        _authenticationService.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task NafathCallback_SignsInFullyOnboardedContractorConsultant_WithoutProfileCompletion()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(), NationalId = "1112223334",
            RoleAssignments = [new UserRoleAssignment { Role = UserRole.ContractorConsultant }],
            FullName = "فهد سالم", IsActive = true, Email = "fahad@example.test",
            Mobile = "0501112233"
        };
        SetupUsersFind([existingUser]);
        _nafathSsoClient.Setup(c => c.GetUserClaimAsync("sso-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NafathUserClaim { NameIdentifier = "1112223334", ArabicName = "فهد سالم" });

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathSsoToken=sso-token-123");
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.ContractorConsultant);

        var result = await controller.NafathCallback(CancellationToken.None);

        Assert.IsType<RedirectResult>(result);
        _authenticationService.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()),
            Times.Once);
        Assert.Empty(_nafathPendingProfileStore.Seeded);
    }


    [Fact]
    public async Task Logout_SignsOutAndSetsReauthCookie()
    {
        var controller = CreateController();

        var result = await controller.Logout();

        _authenticationService.Verify(a => a.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<AuthenticationProperties>()),
            Times.Once);
        Assert.Equal("/login?loggedOut=true", GetRedirectUrl(result));
        Assert.Contains("NafathReauthRequired=1", controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Logout_ReturnsAmanaSloUrl_WhenConfigured()
    {
        const string sloUrl = "https://investment.amana-md.gov.sa/SsoClient/Saml2/Logout";
        var controller = CreateController(nafathSettings: new NafathSettings
        {
            IamSsoUrl = "https://sso.example.test/",
            GetTokenUrl = "Account/GeneraterToken?appkey=",
            UserClaimsUrl = "Account/GetUserClaim?token=",
            AppKey = "test-app-key",
            IamLoginUrl = "https://sso.example.test/Account/IamLogin",
            IamLogoutUrl = sloUrl
        });

        var result = await controller.Logout();

        Assert.Equal(sloUrl, GetRedirectUrl(result));
        _authenticationService.Verify(a => a.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<AuthenticationProperties>()),
            Times.Once);
        Assert.Contains("NafathReauthRequired=1", controller.Response.Headers.SetCookie.ToString());
    }

    private static string GetRedirectUrl(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsType<object>(ok.Value, exactMatch: false);
        return (string)value.GetType().GetProperty("redirectUrl")!.GetValue(value)!;
    }

    [Fact]
    public async Task Logout_MarksResponseNoStore_SoBackButtonCannotRestoreTheApp()
    {
        var controller = CreateController();

        await controller.Logout();

        Assert.Equal("no-cache, no-store, must-revalidate", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Logout_AbandonsInFlightNafathRoundTrip()
    {
        var pendingUserId = Guid.NewGuid();
        _nafathLoginStateStore.Seed("sso-token-123", UserRole.ContractorConsultant);
        await _nafathPendingProfileStore.SetAsync("pending-token-1", pendingUserId, TimeSpan.FromMinutes(10));

        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append(
            "Cookie", "NafathSsoToken=sso-token-123; NafathPendingProfile=pending-token-1");

        await controller.Logout();

        Assert.Null(await _nafathLoginStateStore.GetAndRemoveAsync("sso-token-123"));
        Assert.Null(await _nafathPendingProfileStore.GetAndRemoveAsync("pending-token-1"));
    }

    [Fact]
    public async Task NafathLogin_DoesNotRedirectToAmana_WhenBrowserJustLoggedOut()
    {
        _nafathSsoClient.Setup(c => c.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("sso-token-123");
        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathReauthRequired=1");

        var result = await controller.NafathLogin(UserRole.P3OOfficer, cancellationToken: CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.True((bool)redirect.RouteValues!["confirmReauth"]!);
        Assert.Equal(UserRole.P3OOfficer, redirect.RouteValues!["role"]);
        _nafathSsoClient.Verify(c => c.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NafathLogin_ProceedsAndClearsFlag_WhenUserConfirms()
    {
        _nafathSsoClient.Setup(c => c.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("sso-token-123");
        var controller = CreateController();
        controller.HttpContext.Request.Headers.Append("Cookie", "NafathReauthRequired=1");

        var result = await controller.NafathLogin(UserRole.ContractorConsultant, confirmed: true, cancellationToken: CancellationToken.None);

        Assert.IsType<RedirectResult>(result);
        Assert.Contains("NafathReauthRequired=;", controller.Response.Headers.SetCookie.ToString());
    }

}

internal class FakeNafathLoginStateStore : INafathLoginStateStore
{
    private readonly Dictionary<string, UserRole> _state = [];

    public void Seed(string token, UserRole role) => _state[token] = role;

    public Task SetAsync(string token, UserRole role, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        _state[token] = role;
        return Task.CompletedTask;
    }

    public Task<UserRole?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        if (_state.Remove(token, out var role))
        {
            return Task.FromResult<UserRole?>(role);
        }
        return Task.FromResult<UserRole?>(null);
    }
}

internal class FakeNafathPendingProfileStore : INafathPendingProfileStore
{
    private readonly Dictionary<string, Guid> _state = [];

    public List<Guid> Seeded { get; } = [];

    public Task SetAsync(string token, Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        _state[token] = userId;
        Seeded.Add(userId);
        return Task.CompletedTask;
    }

    public Task<Guid?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        if (_state.Remove(token, out var userId))
        {
            return Task.FromResult<Guid?>(userId);
        }
        return Task.FromResult<Guid?>(null);
    }
}
