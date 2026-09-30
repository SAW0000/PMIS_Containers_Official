using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Queries;

public class AdminUserQueryService(PmisDbContext context) : IAdminUserQueryService
{
    public async Task<PagedAdminUsersDto> GetUsersPageAsync(
        UserRole currentRole, AdminUserListQuery query, CancellationToken cancellationToken = default)
    {
        if (currentRole != UserRole.SystemAdministrator)
        {
            return new PagedAdminUsersDto();
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var baseQuery = context.Users.AsNoTracking().Where(u => !u.DeletedAccount);

        baseQuery = ApplyRoleFilter(baseQuery, query.Roles);
        baseQuery = ApplyActiveFilter(baseQuery, query.Active);
        baseQuery = ApplySearch(baseQuery, query.Search);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var sorted = ApplySort(baseQuery, query.SortCol, query.SortDir);

        var pageOfUsers = await Project(sorted)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedAdminUsersDto
        {
            Items = pageOfUsers.Select(ToDto).ToList(),
            TotalCount = totalCount
        };
    }

    private static IQueryable<UserProjection> Project(IQueryable<User> users) => users
        .Select(u => new UserProjection
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Roles = u.RoleAssignments.Select(ra => ra.Role).ToList(),
            IsActive = u.IsActive,
            PhotoFileRef = u.PhotoFileRef,
            Gender = u.Gender,
            ArabicNationality = u.ArabicNationality,
            Mobile = u.Mobile,
            ArabicFirstName = u.ArabicFirstName,
            ArabicFatherName = u.ArabicFatherName,
            ArabicGrandFatherName = u.ArabicGrandFatherName,
            ArabicFamilyName = u.ArabicFamilyName,
            NationalId = u.NationalId
        });

    private static IQueryable<User> ApplyRoleFilter(IQueryable<User> query, List<string> roleNames)
    {
        if (roleNames is not { Count: > 0 })
        {
            return query.Where(_ => false);
        }

        var roles = roleNames
            .Select(TryParseRoleName)
            .Where(r => r is not null)
            .Select(r => r!.Value)
            .ToList();

        return query.Where(u => u.RoleAssignments.Any(ra => roles.Contains(ra.Role)));
    }

    private static IQueryable<User> ApplyActiveFilter(IQueryable<User> query, string? activeKey) => activeKey switch
    {
        "active" => query.Where(u => u.IsActive),
        "inactive" => query.Where(u => !u.IsActive),
        _ => query
    };

    private static IQueryable<User> ApplySearch(IQueryable<User> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim();
        return query.Where(u =>
            EF.Functions.Like(u.FullName, $"%{term}%") ||
            EF.Functions.Like(u.Email ?? string.Empty, $"%{term}%"));
    }

    private static IQueryable<User> SortByRole(IQueryable<User> query, bool descending) => OrderBy(query, u =>
        u.RoleAssignments.Select(ra =>
            ra.Role == UserRole.SystemAdministrator ? 0
                : ra.Role == UserRole.P3OOfficer ? 1
                : ra.Role == UserRole.ContractorConsultant ? 2
                : 3).Min(),
        descending);

    private static IQueryable<User> ApplySort(IQueryable<User> query, string? sortCol, string? sortDir)
    {
        var descending = sortDir == "desc";

        return sortCol switch
        {
            "email" => OrderBy(query, u => u.Email, descending),
            "role" => SortByRole(query, descending),
            "status" => OrderBy(query, u => u.IsActive, descending),
            "gender" => OrderBy(query, u => u.Gender, descending),
            "nationality" => OrderBy(query, u => u.ArabicNationality, descending),
            "mobile" => OrderBy(query, u => u.Mobile, descending),
            _ => OrderBy(query, u => u.FullName, descending)
        };
    }

    private static IQueryable<User> OrderBy<TKey>(
        IQueryable<User> query, System.Linq.Expressions.Expression<Func<User, TKey>> keySelector, bool descending)
        => descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    private static UserRole? TryParseRoleName(string roleName) => Enum.TryParse<UserRole>(roleName, out var role) ? role : null;

    private static AdminUserListItemDto ToDto(UserProjection u) => new()
    {
        Id = u.Id,
        Name = u.FullName,
        Email = u.Email ?? string.Empty,
        Roles = u.Roles.Select(r => r.ToString()).ToList(),
        Active = u.IsActive,
        HasPhoto = !string.IsNullOrWhiteSpace(u.PhotoFileRef),
        Gender = GenderFormat.ToArabicLabel(u.Gender),
        ArabicNationality = u.ArabicNationality ?? string.Empty,
        Mobile = u.Mobile ?? string.Empty,
        ArabicFirstName = u.ArabicFirstName ?? string.Empty,
        ArabicFatherName = u.ArabicFatherName ?? string.Empty,
        ArabicGrandFatherName = u.ArabicGrandFatherName ?? string.Empty,
        ArabicFamilyName = u.ArabicFamilyName ?? string.Empty,
        NationalId = u.NationalId ?? string.Empty
    };

    private sealed class UserProjection
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public List<UserRole> Roles { get; set; } = [];
        public bool IsActive { get; set; }
        public string? PhotoFileRef { get; set; }
        public Gender? Gender { get; set; }
        public string? ArabicNationality { get; set; }
        public string? Mobile { get; set; }
        public string? ArabicFirstName { get; set; }
        public string? ArabicFatherName { get; set; }
        public string? ArabicGrandFatherName { get; set; }
        public string? ArabicFamilyName { get; set; }
        public string? NationalId { get; set; }
    }
}
