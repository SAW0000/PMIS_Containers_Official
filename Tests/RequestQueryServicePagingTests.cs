using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Queries;

namespace PMIS.Tests;

public class RequestQueryServicePagingTests
{
    private static PmisDbContext NewContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new PmisDbContext(options);
    }

    private static RequestQueryService NewService(PmisDbContext context) => new(context, new NoOpReferenceDataCache());

    private sealed class NoOpReferenceDataCache : IReferenceDataCache
    {
        public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl) => Task.CompletedTask;

        public Task InvalidateAsync(string key) => Task.CompletedTask;
    }

    private static (User User, Company Company, Project Project) SeedApplicant(
        PmisDbContext context, string fullName, string companyName, string projectName)
    {
        var user = new User { FullName = fullName, Email = $"{Guid.NewGuid()}@example.com" };
        var company = new Company { CompanyName = companyName };
        var project = new Project { ProjectName = projectName, ContractNumber = "C-1" };
        context.Users.Add(user);
        context.Companies.Add(company);
        context.Projects.Add(project);
        return (user, company, project);
    }

    private static Request MakeRequest(
        User user, Company company, Project project, string requestNumber,
        RequestStatus status, DateTime requestDate) => new()
    {
        RequestNumber = requestNumber,
        UserId = user.Id,
        User = user,
        CompanyId = company.Id,
        Company = company,
        ProjectId = project.Id,
        Project = project,
        Status = status,
        RequestDate = requestDate,
        IsActive = true
    };

    [Fact]
    public async Task GetVisibleRequestsPageAsync_ScopesContractorConsultantToOwnRequests()
    {
        var context = NewContext();
        var (ownerA, companyA, projectA) = SeedApplicant(context, "Owner A", "Co A", "Proj A");
        var (ownerB, companyB, projectB) = SeedApplicant(context, "Owner B", "Co B", "Proj B");
        context.Requests.Add(MakeRequest(ownerA, companyA, projectA, "REQ-1", RequestStatus.Submitted, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(ownerB, companyB, projectB, "REQ-2", RequestStatus.Submitted, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = NewService(context);
        var query = new RequestListQuery { Statuses = ["submitted"] };

        var page = await service.GetVisibleRequestsPageAsync(ownerA.Id, UserRole.ContractorConsultant, query);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("REQ-1", Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task GetVisibleRequestsPageAsync_EmptyStatusesMatchesNoRows()
    {
        var context = NewContext();
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-1", RequestStatus.Submitted, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = NewService(context);
        var query = new RequestListQuery { Statuses = [] };

        var page = await service.GetVisibleRequestsPageAsync(owner.Id, UserRole.P3OOfficer, query);

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task GetVisibleRequestsPageAsync_SearchMatchesApplicantCompanyProjectOrRequestNumber()
    {
        var context = NewContext();
        var (owner, company, project) = SeedApplicant(context, "Ahmed Ali", "Acme Co", "Runway Upgrade");
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-42", RequestStatus.Submitted, DateTime.UtcNow));
        var (otherOwner, otherCompany, otherProject) = SeedApplicant(context, "Sara Noor", "Other Co", "Other Proj");
        context.Requests.Add(MakeRequest(otherOwner, otherCompany, otherProject, "REQ-99", RequestStatus.Submitted, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = NewService(context);
        var allStatuses = new List<string> { "submitted" };

        var byApplicant = await service.GetVisibleRequestsPageAsync(
            owner.Id, UserRole.P3OOfficer, new RequestListQuery { Statuses = allStatuses, Search = "Ahmed" });
        Assert.Equal("REQ-42", Assert.Single(byApplicant.Items).Id);

        var byRequestNumber = await service.GetVisibleRequestsPageAsync(
            owner.Id, UserRole.P3OOfficer, new RequestListQuery { Statuses = allStatuses, Search = "REQ-99" });
        Assert.Equal("REQ-99", Assert.Single(byRequestNumber.Items).Id);
    }

    [Fact]
    public async Task GetVisibleRequestsPageAsync_SortsByStatusUsingMockDisplayOrder()
    {
        var context = NewContext();
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-ACTIVATED", RequestStatus.Activated, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-DRAFT", RequestStatus.Draft, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-AWAITING", RequestStatus.AwaitingPaymentConfirmation, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = NewService(context);
        var query = new RequestListQuery
        {
            Statuses = ["draft", "activated", "awaiting_payment"],
            SortCol = "status",
            SortDir = "asc",
            PageSize = 10
        };

        var page = await service.GetVisibleRequestsPageAsync(owner.Id, UserRole.P3OOfficer, query);

        Assert.Equal(["REQ-DRAFT", "REQ-AWAITING", "REQ-ACTIVATED"], page.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetVisibleRequestsPageAsync_PagesResultsAndReportsTotalCountAcrossAllPages()
    {
        var context = NewContext();
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        for (var i = 0; i < 15; i++)
        {
            context.Requests.Add(MakeRequest(
                owner, company, project, $"REQ-{i:00}", RequestStatus.Submitted, DateTime.UtcNow.AddMinutes(i)));
        }
        await context.SaveChangesAsync();

        var service = NewService(context);
        var query = new RequestListQuery
        {
            Statuses = ["submitted"], Page = 2, PageSize = 10, SortCol = "date", SortDir = "asc"
        };

        var page = await service.GetVisibleRequestsPageAsync(owner.Id, UserRole.P3OOfficer, query);

        Assert.Equal(15, page.TotalCount);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal("REQ-10", page.Items[0].Id);
    }

    [Fact]
    public async Task GetVisibleRequestsPageAsync_ItemsCarryNoNotesOrHistory()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seedContext = NewContext(databaseName);
        var (owner, company, project) = SeedApplicant(seedContext, "Owner", "Co", "Proj");
        var request = MakeRequest(owner, company, project, "REQ-1", RequestStatus.Submitted, DateTime.UtcNow);
        seedContext.Requests.Add(request);
        var reason = new RejectionReasonOption { Label = "سبب" };
        seedContext.RejectionReasonOptions.Add(reason);
        await seedContext.SaveChangesAsync();
        seedContext.RequestNotes.Add(new RequestNote
        {
            RequestId = request.Id, UserId = owner.Id, ReasonId = reason.Id, NoteText = "note"
        });
        seedContext.RequestHistories.Add(new RequestHistory { RequestId = request.Id, EventLabel = "إرسال الطلب", EventDate = DateTime.UtcNow });
        await seedContext.SaveChangesAsync();

        var readContext = NewContext(databaseName);
        var service = NewService(readContext);
        var query = new RequestListQuery { Statuses = ["submitted"] };

        var page = await service.GetVisibleRequestsPageAsync(owner.Id, UserRole.P3OOfficer, query);

        var item = Assert.Single(page.Items);
        Assert.Empty(item.Notes);
        Assert.Empty(item.History);
    }
}
