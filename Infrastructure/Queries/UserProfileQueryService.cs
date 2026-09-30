using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Queries;

public class UserProfileQueryService(PmisDbContext context) : IUserProfileQueryService
{
    public async Task<CurrentUserProfileDto?> GetOwnProfileAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.DeletedAccount)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Mobile,
                u.CreatedDate,
                u.PhotoFileRef,
                u.IsActive,
                u.ArabicFirstName,
                u.ArabicFatherName,
                u.ArabicGrandFatherName,
                u.ArabicFamilyName,
                u.ArabicNationality,
                u.Gender
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roleLabel = role switch
        {
            UserRole.ContractorConsultant => "مقاول / استشاري",
            UserRole.P3OOfficer => "موظف مكتب P3O",
            UserRole.SystemAdministrator => "مدير النظام",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unmapped user role")
        };

        return new CurrentUserProfileDto
        {
            Id = user.Id,
            Name = user.FullName,
            Avatar = AvatarInitials(user.FullName),
            HasPhoto = !string.IsNullOrWhiteSpace(user.PhotoFileRef),
            RoleLabel = roleLabel,
            Email = user.Email ?? string.Empty,
            Mobile = user.Mobile ?? string.Empty,
            Joined = user.CreatedDate,
            IsActive = user.IsActive,
            FirstName = user.ArabicFirstName ?? string.Empty,
            LastName = user.ArabicFamilyName ?? string.Empty,
            FatherName = user.ArabicFatherName ?? string.Empty,
            GrandFatherName = user.ArabicGrandFatherName ?? string.Empty,
            Nationality = user.ArabicNationality ?? string.Empty,
            Gender = GenderFormat.ToArabicLabel(user.Gender)
        };
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
