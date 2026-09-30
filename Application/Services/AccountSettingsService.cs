using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public partial class AccountSettingsService(
    PmisDbContext context, IUnitOfWork unitOfWork,
    IUserPhotoUploadService photoUploadService, IAuditLogRepository auditLogRepository,
    INotificationService notificationService, IOtpService otpService) : IAccountSettingsService
{
    private static string AccountOtpContext(Guid userId) => $"account:{userId}";

    public async Task<(AccountContactVerificationResultDto? Result, string? Error)> SendContactVerificationAsync(
        Guid userId, AccountContactVerificationRequestDto dto, bool isResend, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAccountContact(dto.Email, dto.Mobile);
        if (validationError is not null) return (null, validationError);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.DeletedAccount, cancellationToken);
        if (user is null) return (null, "المستخدم غير موجود");

        var email = dto.Email.Trim();
        var mobile = dto.Mobile.Trim();
        var emailChanged = email != (user.Email ?? string.Empty);
        var mobileChanged = mobile != (user.Mobile ?? string.Empty);

        if (emailChanged)
        {
            var emailTaken = await context.Users.AnyAsync(u => u.Id != userId && u.Email == email, cancellationToken);
            if (emailTaken) return (null, "هذا البريد الإلكتروني مستخدم بالفعل");
        }

        var contextId = AccountOtpContext(userId);
        var result = new AccountContactVerificationResultDto();

        if (emailChanged)
        {
            var error = await SendOneCodeAsync(contextId, OtpChannel.Email, email, isResend, cancellationToken);
            if (error is not null) return (null, error);
            result.EmailCodeSent = true;
        }

        if (mobileChanged)
        {
            var error = await SendOneCodeAsync(contextId, OtpChannel.Mobile, mobile, isResend, cancellationToken);
            if (error is not null) return (null, error);
            result.MobileCodeSent = true;
        }

        return (result, null);
    }

    private async Task<string?> SendOneCodeAsync(
        string contextId, OtpChannel channel, string destination, bool isResend, CancellationToken cancellationToken)
    {
        var sendResult = isResend
            ? await otpService.ResendAsync(contextId, channel, destination, cancellationToken)
            : await otpService.SendAsync(contextId, channel, destination, cancellationToken);
        return OtpResultMessages.ForSendFailure(sendResult);
    }

    private async Task<string?> VerifyOneCodeAsync(
        string contextId, OtpChannel channel, string? code, CancellationToken cancellationToken)
    {
        var verifyResult = await otpService.VerifyAsync(contextId, channel, code ?? string.Empty, cancellationToken);
        return OtpResultMessages.ForVerifyFailure(verifyResult);
    }

    public async Task<(AccountInfoUpdateResultDto? Result, string? Error)> UpdateAccountInfoAsync(
        Guid userId, AccountInfoUpdateDto dto, ScannedPhoto? photo, string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAccountInfo(dto);
        if (validationError is not null) return (null, validationError);

        var user = await context.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.DeletedAccount, cancellationToken);
        if (user is null) return (null, "المستخدم غير موجود");

        var email = dto.Email.Trim();
        var emailTaken = await context.Users
            .AnyAsync(u => u.Id != userId && u.Email == email, cancellationToken);
        if (emailTaken) return (null, "هذا البريد الإلكتروني مستخدم بالفعل");

        var contextId = AccountOtpContext(userId);
        var mobile = dto.Mobile.Trim();

        if (email != (user.Email ?? string.Empty))
        {
            var error = await VerifyOneCodeAsync(contextId, OtpChannel.Email, dto.EmailCode, cancellationToken);
            if (error is not null) return (null, error);
        }

        if (mobile != (user.Mobile ?? string.Empty))
        {
            var error = await VerifyOneCodeAsync(contextId, OtpChannel.Mobile, dto.MobileCode, cancellationToken);
            if (error is not null) return (null, error);
        }

        var previousEmail = user.Email;
        var previousPhotoRef = user.PhotoFileRef;

        user.Email = email;
        user.Mobile = dto.Mobile.Trim();

        string? supersededPhotoRef = null;
        if (photo is not null)
        {
            supersededPhotoRef = await photoUploadService.AttachToUserAsync(photo, user, cancellationToken);
        }
        else if (dto.RemovePhoto && !string.IsNullOrWhiteSpace(user.PhotoFileRef))
        {
            supersededPhotoRef = user.PhotoFileRef;
            user.PhotoFileRef = null;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(supersededPhotoRef))
        {
            await photoUploadService.DeleteSupersededAsync(supersededPhotoRef, user.Id, cancellationToken);
        }

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "User",
            EntityId = user.Id,
            Action = "UserUpdated",
            PerformedBy = new AuditLogActor
            {
                UserId = user.Id,
                FullNameSnapshot = user.FullName,
                Role = user.PrimaryRole?.ToString() ?? string.Empty
            },
            Note = DescribeChanges(previousEmail, user.Email, user.PhotoFileRef != previousPhotoRef),
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress
        }, cancellationToken);

        await notificationService.NotifyAccountChangedAsync(user, user.Id, cancellationToken);

        return (new AccountInfoUpdateResultDto
        {
            Name = user.FullName,
            Email = user.Email ?? string.Empty,
            Mobile = user.Mobile ?? string.Empty,
            Avatar = AvatarInitials(user.FullName),
            HasPhoto = !string.IsNullOrWhiteSpace(user.PhotoFileRef),
            IdentityRefreshed = previousEmail != user.Email
        }, null);
    }

    private static string? ValidateAccountInfo(AccountInfoUpdateDto dto) =>
        ValidateAccountContact(dto.Email, dto.Mobile);

    private static string? ValidateAccountContact(string? email, string? mobile)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(mobile))
        {
            return "جميع الحقول مطلوبة";
        }

        if (!EmailFormat.IsValid(email.Trim()))
        {
            return "صيغة البريد الإلكتروني غير صحيحة";
        }

        if (!MobileFormat.IsValid(mobile.Trim()))
        {
            return "رقم الجوال غير صحيح — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام";
        }

        return null;
    }

    private static string DescribeChanges(string? previousEmail, string newEmail, bool photoChanged)
    {
        var changed = new List<string>();
        if (previousEmail != newEmail) changed.Add("البريد الإلكتروني");
        if (photoChanged) changed.Add("الصورة الشخصية");

        return changed.Count == 0
            ? "تحديث بيانات الحساب"
            : $"تحديث بيانات الحساب — {string.Join("، ", changed)}";
    }

    private static string AvatarInitials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "؟",
            1 => parts[0][..1],
            _ => parts[0][..1] + parts[1][..1]
        };
    }
}
