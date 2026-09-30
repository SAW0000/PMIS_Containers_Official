using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public class AdminUserService(
    PmisDbContext context, IUnitOfWork unitOfWork,
    IAuditLogRepository auditLogRepository, INotificationService notificationService,
    IUserPhotoUploadService photoUploadService)
    : IAdminUserService
{
    public async Task<string?> UpdateAsync(Guid id, AdminUserUpdateDto dto, RequestActor actor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email)
            || string.IsNullOrWhiteSpace(dto.Mobile) || string.IsNullOrWhiteSpace(dto.NationalId))
        {
            return "الاسم والبريد الإلكتروني ورقم الجوال ورقم الهوية مطلوبة";
        }

        if (!EmailFormat.IsValid(dto.Email))
        {
            return "صيغة البريد الإلكتروني غير صحيحة";
        }

        var mobile = dto.Mobile.Trim();
        if (!MobileFormat.IsValid(mobile))
        {
            return "رقم الجوال غير صحيح — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام";
        }

        var nationalId = dto.NationalId.Trim();
        if (!NationalIdFormat.IsValid(nationalId))
        {
            return "رقم الهوية الوطنية/الإقامة غير صحيح — يجب أن يتكون من 10 أرقام ويبدأ بـ 1 أو 2";
        }

        if (!TryParseGender(dto.Gender, out var gender, out var genderError))
        {
            return genderError;
        }

        if (!TryParseRoles(dto.Roles, out var roles, out var roleError))
        {
            return roleError;
        }

        var user = await context.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == id && !u.DeletedAccount, cancellationToken);
        if (user is null) return "المستخدم غير موجود";

        var email = dto.Email.Trim();
        var emailTaken = await context.Users
            .AnyAsync(u => u.Id != id && u.Email == email, cancellationToken);
        if (emailTaken) return "هذا البريد الإلكتروني مستخدم بالفعل";

        var nationalIdTaken = await context.Users
            .AnyAsync(u => u.Id != id && u.NationalId == nationalId, cancellationToken);
        if (nationalIdTaken) return "رقم الهوية الوطنية مستخدم بالفعل";

        user.FullName = dto.Name.Trim();
        user.Email = email;
        user.Mobile = mobile;
        user.NationalId = nationalId;
        user.ArabicFirstName = NullIfBlank(dto.ArabicFirstName);
        user.ArabicFatherName = NullIfBlank(dto.ArabicFatherName);
        user.ArabicGrandFatherName = NullIfBlank(dto.ArabicGrandFatherName);
        user.ArabicFamilyName = NullIfBlank(dto.ArabicFamilyName);
        user.ArabicNationality = NullIfBlank(dto.ArabicNationality);
        user.Gender = gender;
        ReplaceRoleAssignments(user, roles);
        user.IsActive = dto.Active;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "User",
            EntityId = user.Id,
            Action = "UserUpdated",
            PerformedBy = new AuditLogActor { UserId = actor.UserId, FullNameSnapshot = actor.FullName, Role = actor.Role.ToString() },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        await notificationService.NotifyAccountChangedAsync(user, actor.UserId, cancellationToken);

        if (user.Id != actor.UserId)
        {
            await notificationService.NotifyOwnAccountChangedAsync(user, cancellationToken);
        }

        return null;
    }

    public async Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.DeletedAccount, cancellationToken);
        if (user is null) return "المستخدم غير موجود";

        user.DeletedAccount = true;
        user.IsActive = false;

        var photoRef = user.PhotoFileRef;
        user.PhotoFileRef = null;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(photoRef))
        {
            await photoUploadService.DeleteSupersededAsync(photoRef, user.Id, cancellationToken);
        }

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "User",
            EntityId = user.Id,
            Action = "UserDeleted",
            PerformedBy = new AuditLogActor { UserId = actor.UserId, FullNameSnapshot = actor.FullName, Role = actor.Role.ToString() },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        await notificationService.NotifyAccountChangedAsync(user, actor.UserId, cancellationToken);

        return null;
    }

    public async Task<string?> CreateAsync(AdminUserCreateDto dto, RequestActor actor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName)
            || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Mobile)
            || string.IsNullOrWhiteSpace(dto.NationalId))
        {
            return "جميع الحقول مطلوبة";
        }

        if (!EmailFormat.IsValid(dto.Email))
        {
            return "صيغة البريد الإلكتروني غير صحيحة";
        }

        if (!MobileFormat.IsValid(dto.Mobile))
        {
            return "رقم الجوال غير صحيح — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام";
        }

        if (!NationalIdFormat.IsValid(dto.NationalId))
        {
            return "رقم الهوية الوطنية/الإقامة غير صحيح — يجب أن يتكون من 10 أرقام ويبدأ بـ 1 أو 2";
        }

        if (!TryParseRoles(dto.Roles, out var roles, out var roleError))
        {
            return roleError;
        }

        var email = dto.Email.Trim();
        var nationalId = dto.NationalId.Trim();

        var emailTaken = await context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken) return "هذا البريد الإلكتروني مسجل بالفعل";

        var existingByNationalId = await context.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.NationalId == nationalId, cancellationToken);
        if (existingByNationalId is not null && !existingByNationalId.DeletedAccount)
        {
            return "رقم الهوية الوطنية مستخدم بالفعل";
        }

        if (existingByNationalId is { DeletedAccount: true } revived)
        {
            revived.DeletedAccount = false;
            revived.FullName = $"{dto.FirstName.Trim()} {dto.LastName.Trim()}";
            revived.Email = email;
            revived.Mobile = dto.Mobile.Trim();
            ReplaceRoleAssignments(revived, roles);
            revived.IsActive = true;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditLogRepository.AddAsync(new AuditLogEntry
            {
                EntityType = "User",
                EntityId = revived.Id,
                Action = "UserCreated",
                PerformedBy = new AuditLogActor { UserId = actor.UserId, FullNameSnapshot = actor.FullName, Role = actor.Role.ToString() },
                Timestamp = DateTime.UtcNow,
                IpAddress = actor.IpAddress
            }, cancellationToken);

            await notificationService.NotifyAccountChangedAsync(revived, actor.UserId, cancellationToken);

            return null;
        }

        var user = new User
        {
            FullName = $"{dto.FirstName.Trim()} {dto.LastName.Trim()}",
            Email = email,
            Mobile = dto.Mobile.Trim(),
            NationalId = nationalId,
            IsActive = true
        };
        ReplaceRoleAssignments(user, roles);

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "User",
            EntityId = user.Id,
            Action = "UserCreated",
            PerformedBy = new AuditLogActor { UserId = actor.UserId, FullNameSnapshot = actor.FullName, Role = actor.Role.ToString() },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        await notificationService.NotifyAccountChangedAsync(user, actor.UserId, cancellationToken);

        return null;
    }

    private static bool TryParseRoles(List<string> roleNames, out List<UserRole> roles, out string? error)
    {
        roles = [];
        error = null;

        if (roleNames.Count == 0)
        {
            error = "الصفة غير صحيحة";
            return false;
        }

        foreach (var name in roleNames)
        {
            if (!Enum.TryParse<UserRole>(name, out var role))
            {
                error = "الصفة غير صحيحة";
                return false;
            }

            roles.Add(role);
        }

        return true;
    }

    private static bool TryParseGender(string? gender, out Gender? parsed, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(gender))
        {
            parsed = null;
            return true;
        }

        if (!GenderFormat.TryParseArabicLabel(gender, out var value))
        {
            parsed = null;
            error = "قيمة الجنس غير صحيحة";
            return false;
        }

        parsed = value;
        return true;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void ReplaceRoleAssignments(User user, IEnumerable<UserRole> roles)
    {
        context.UserRoleAssignments.RemoveRange(user.RoleAssignments);
        user.RoleAssignments.Clear();
        foreach (var role in roles.Distinct())
        {
            user.RoleAssignments.Add(new UserRoleAssignment { Role = role });
        }
    }
}
