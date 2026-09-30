using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Queries;

namespace PMIS.Tests;

public class AdminUserQueryServicePagingTests
{
    private static PmisDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PmisDbContext(options);
    }

    private static User MakeUser(string fullName, string email, bool isActive, params UserRole[] roles)
    {
        var user = new User { FullName = fullName, Email = email, IsActive = isActive };
        user.RoleAssignments = roles.Select(r => new UserRoleAssignment { UserId = user.Id, User = user, Role = r }).ToList();
        return user;
    }

    [Fact]
    public async Task GetUsersPageAsync_NonAdminGetsEmptyPage()
    {
        var context = NewContext();
        context.Users.Add(MakeUser("Ahmed", "ahmed@example.com", true, UserRole.P3OOfficer));
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var page = await service.GetUsersPageAsync(UserRole.P3OOfficer, new AdminUserListQuery());

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task GetUsersPageAsync_EmptyRolesMatchesNoRows()
    {
        var context = NewContext();
        context.Users.Add(MakeUser("Ahmed", "ahmed@example.com", true, UserRole.P3OOfficer));
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var query = new AdminUserListQuery { Roles = [] };

        var page = await service.GetUsersPageAsync(UserRole.SystemAdministrator, query);

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task GetUsersPageAsync_ExcludesDeletedAccounts()
    {
        var context = NewContext();
        var deleted = MakeUser("Deleted Guy", "deleted@example.com", true, UserRole.P3OOfficer);
        deleted.DeletedAccount = true;
        context.Users.Add(deleted);
        context.Users.Add(MakeUser("Live Guy", "live@example.com", true, UserRole.P3OOfficer));
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var query = new AdminUserListQuery { Roles = ["P3OOfficer", "SystemAdministrator", "ContractorConsultant"] };

        var page = await service.GetUsersPageAsync(UserRole.SystemAdministrator, query);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Live Guy", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetUsersPageAsync_SearchMatchesNameOrEmail()
    {
        var context = NewContext();
        context.Users.Add(MakeUser("Ahmed Ali", "ahmed.ali@example.com", true, UserRole.P3OOfficer));
        context.Users.Add(MakeUser("Sara Noor", "sara.noor@example.com", true, UserRole.P3OOfficer));
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var allRoles = new List<string> { "P3OOfficer", "SystemAdministrator", "ContractorConsultant" };

        var byName = await service.GetUsersPageAsync(
            UserRole.SystemAdministrator, new AdminUserListQuery { Roles = allRoles, Search = "Ahmed" });
        Assert.Equal("Ahmed Ali", Assert.Single(byName.Items).Name);

        var byEmail = await service.GetUsersPageAsync(
            UserRole.SystemAdministrator, new AdminUserListQuery { Roles = allRoles, Search = "sara.noor" });
        Assert.Equal("Sara Noor", Assert.Single(byEmail.Items).Name);
    }

    [Fact]
    public async Task GetUsersPageAsync_ActiveFilterNarrowsToActiveOrInactive()
    {
        var context = NewContext();
        context.Users.Add(MakeUser("Active Guy", "active@example.com", true, UserRole.P3OOfficer));
        context.Users.Add(MakeUser("Inactive Guy", "inactive@example.com", false, UserRole.P3OOfficer));
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var allRoles = new List<string> { "P3OOfficer", "SystemAdministrator", "ContractorConsultant" };

        var activeOnly = await service.GetUsersPageAsync(
            UserRole.SystemAdministrator, new AdminUserListQuery { Roles = allRoles, Active = "active" });
        Assert.Equal("Active Guy", Assert.Single(activeOnly.Items).Name);

        var inactiveOnly = await service.GetUsersPageAsync(
            UserRole.SystemAdministrator, new AdminUserListQuery { Roles = allRoles, Active = "inactive" });
        Assert.Equal("Inactive Guy", Assert.Single(inactiveOnly.Items).Name);
    }

    [Fact]
    public async Task GetUsersPageAsync_SortsByRoleUsingDisplayRankNotEnumOrder()
    {
        var context = NewContext();
        context.Users.Add(MakeUser("Contractor", "contractor@example.com", true, UserRole.ContractorConsultant));
        context.Users.Add(MakeUser("Admin", "admin@example.com", true, UserRole.SystemAdministrator));
        context.Users.Add(MakeUser("DualRole", "dual@example.com", true, UserRole.ContractorConsultant, UserRole.P3OOfficer));
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var query = new AdminUserListQuery
        {
            Roles = ["P3OOfficer", "SystemAdministrator", "ContractorConsultant"],
            SortCol = "role",
            SortDir = "asc",
            PageSize = 10
        };

        var page = await service.GetUsersPageAsync(UserRole.SystemAdministrator, query);

        Assert.Equal(["Admin", "DualRole", "Contractor"], page.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetUsersPageAsync_PagesResultsAndReportsTotalCountAcrossAllPages()
    {
        var context = NewContext();
        for (var i = 0; i < 15; i++)
        {
            context.Users.Add(MakeUser($"User {i:00}", $"user{i:00}@example.com", true, UserRole.P3OOfficer));
        }
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var query = new AdminUserListQuery
        {
            Roles = ["P3OOfficer"], Page = 2, PageSize = 10, SortCol = "name", SortDir = "asc"
        };

        var page = await service.GetUsersPageAsync(UserRole.SystemAdministrator, query);

        Assert.Equal(15, page.TotalCount);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal("User 10", page.Items[0].Name);
    }

    [Fact]
    public async Task GetUsersPageAsync_SearchDoesNotMatchRequestCompanyOrOrg()
    {
        var context = NewContext();
        var contractor = MakeUser("Contractor", "contractor@example.com", true, UserRole.ContractorConsultant);
        var officer = MakeUser("Officer", "officer@example.com", true, UserRole.P3OOfficer);
        context.Users.AddRange(contractor, officer);

        var company = new Company { CompanyName = "New Co" };
        var project = new Project { ProjectName = "Proj", ContractNumber = "C-1" };
        context.Companies.Add(company);
        context.Projects.Add(project);
        context.Requests.Add(new Request
        {
            RequestNumber = "REQ-NEW", UserId = contractor.Id, User = contractor,
            CompanyId = company.Id, Company = company, ProjectId = project.Id, Project = project,
            Status = RequestStatus.Submitted, RequestDate = DateTime.UtcNow, IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new AdminUserQueryService(context);
        var allRoles = new List<string> { "ContractorConsultant", "P3OOfficer" };

        var byCompanyName = await service.GetUsersPageAsync(
            UserRole.SystemAdministrator, new AdminUserListQuery { Roles = allRoles, Search = "New Co" });
        Assert.Empty(byCompanyName.Items);

        var byMunicipality = await service.GetUsersPageAsync(
            UserRole.SystemAdministrator, new AdminUserListQuery { Roles = allRoles, Search = "أمانة منطقة المدينة المنورة" });
        Assert.Empty(byMunicipality.Items);
    }
}
