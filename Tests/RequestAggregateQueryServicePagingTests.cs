using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Persistence.Configurations;
using PMIS.Infrastructure.Queries;

namespace PMIS.Tests;

public class RequestAggregateQueryServicePagingTests
{
    private static PmisDbContext NewContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new PmisDbContext(options);
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

    private static async Task SeedSystemSettingsAsync(PmisDbContext context, int responseSlaBusinessDays = 3)
    {
        context.SystemSettings.Add(new SystemSettings { Id = SystemSettingsConfiguration.SingletonId, ResponseSlaBusinessDays = responseSlaBusinessDays });
        await context.SaveChangesAsync();
    }


    [Fact]
    public async Task GetDashboardSummaryAsync_ScopesContractorConsultantToOwnRequests()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (ownerA, companyA, projectA) = SeedApplicant(context, "Owner A", "Co A", "Proj A");
        var (ownerB, companyB, projectB) = SeedApplicant(context, "Owner B", "Co B", "Proj B");
        context.Requests.Add(MakeRequest(ownerA, companyA, projectA, "REQ-1", RequestStatus.Submitted, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(ownerB, companyB, projectB, "REQ-2", RequestStatus.Submitted, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(ownerA.Id, UserRole.ContractorConsultant, new DashboardSummaryQuery { Period = "all" });

        Assert.Equal(1, summary.Stats.Total);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_P3OOfficerSeesEveryonesRequests()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (ownerA, companyA, projectA) = SeedApplicant(context, "Owner A", "Co A", "Proj A");
        var (ownerB, companyB, projectB) = SeedApplicant(context, "Owner B", "Co B", "Proj B");
        context.Requests.Add(MakeRequest(ownerA, companyA, projectA, "REQ-1", RequestStatus.Submitted, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(ownerB, companyB, projectB, "REQ-2", RequestStatus.Submitted, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(ownerA.Id, UserRole.P3OOfficer, new DashboardSummaryQuery { Period = "all" });

        Assert.Equal(2, summary.Stats.Total);
    }


    [Fact]
    public async Task StatusBars_IncludeAllEightStatusesInOrderWithZeroCounts()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-1", RequestStatus.Draft, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-2", RequestStatus.Submitted, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-3", RequestStatus.Submitted, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(owner.Id, UserRole.P3OOfficer, new DashboardSummaryQuery { Period = "all" });

        Assert.Equal(
            ["draft", "submitted", "returned", "rejected", "awaiting_payment", "approved", "activated", "cancelled"],
            summary.StatusBars.Counts.Select(c => c.StatusKey));
        Assert.Equal([1, 2, 0, 0, 0, 0, 0, 0], summary.StatusBars.Counts.Select(c => c.Count));
    }


    [Fact]
    public async Task Sla_ExcludesRequestsWithNoRespondingEventYet()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context, responseSlaBusinessDays: 3);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var resolved = MakeRequest(owner, company, project, "REQ-RESOLVED", RequestStatus.Rejected, DateTime.UtcNow);
        var inFlight = MakeRequest(owner, company, project, "REQ-INFLIGHT", RequestStatus.Submitted, DateTime.UtcNow);
        context.Requests.AddRange(resolved, inFlight);
        await context.SaveChangesAsync();

        var submitDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.RequestHistories.Add(new RequestHistory { RequestId = resolved.Id, EventLabel = "إرسال الطلب", EventDate = submitDate });
        context.RequestHistories.Add(new RequestHistory { RequestId = resolved.Id, EventLabel = "رفض الطلب", EventDate = submitDate.AddDays(2) });
        context.RequestHistories.Add(new RequestHistory { RequestId = inFlight.Id, EventLabel = "إرسال الطلب", EventDate = submitDate });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(owner.Id, UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all" });

        Assert.Equal(1, summary.Sla.TotalRespondedCount);
        Assert.Equal(1, summary.Sla.WithinCount);
    }

    [Fact]
    public async Task Sla_TargetDaysReadsRealSystemSetting_NotHardcodedThree()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context, responseSlaBusinessDays: 5);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var request = MakeRequest(owner, company, project, "REQ-1", RequestStatus.Rejected, DateTime.UtcNow);
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var submitDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.RequestHistories.Add(new RequestHistory { RequestId = request.Id, EventLabel = "إرسال الطلب", EventDate = submitDate });
        context.RequestHistories.Add(new RequestHistory { RequestId = request.Id, EventLabel = "رفض الطلب", EventDate = submitDate.AddDays(4) });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(owner.Id, UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all" });

        Assert.Equal(5, summary.Sla.TargetDays);
        Assert.Equal(1, summary.Sla.WithinCount);
    }

    [Fact]
    public async Task Trend_ExcludesUnresolvedRequestsFromMonthlyAverage()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var resolved = MakeRequest(owner, company, project, "REQ-RESOLVED", RequestStatus.Activated, DateTime.UtcNow);
        var inFlight = MakeRequest(owner, company, project, "REQ-INFLIGHT", RequestStatus.Submitted, DateTime.UtcNow);
        context.Requests.AddRange(resolved, inFlight);
        await context.SaveChangesAsync();

        var submitDate = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc);
        context.RequestHistories.Add(new RequestHistory { RequestId = resolved.Id, EventLabel = "إرسال الطلب", EventDate = submitDate });
        context.RequestHistories.Add(new RequestHistory { RequestId = resolved.Id, EventLabel = "تفعيل الرخصة", EventDate = submitDate.AddDays(6) });
        context.RequestHistories.Add(new RequestHistory { RequestId = inFlight.Id, EventLabel = "إرسال الطلب", EventDate = submitDate });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(owner.Id, UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all" });

        var june = Assert.Single(summary.Trend, p => p.MonthKey == "2026-06");
        Assert.Equal(6, june.AvgDays);
    }


    [Fact]
    public async Task ActionItems_PagesAndReportsTotalCount()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        for (var i = 0; i < 5; i++)
        {
            context.Requests.Add(MakeRequest(owner, company, project, $"REQ-{i}", RequestStatus.Draft, DateTime.UtcNow.AddMinutes(i)));
        }
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(
            owner.Id, UserRole.ContractorConsultant, new DashboardSummaryQuery { Period = "all", ActionPage = 2, ActionPageSize = 2 });

        Assert.Equal(5, summary.ActionItems.TotalCount);
        Assert.Equal(2, summary.ActionItems.Items.Count);
    }

    [Fact]
    public async Task ActionItems_ReturnedItemCarriesLatestNoteText()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var request = MakeRequest(owner, company, project, "REQ-1", RequestStatus.ReturnedForCorrection, DateTime.UtcNow);
        context.Requests.Add(request);
        var reason = new RejectionReasonOption { Label = "بيانات ناقصة" };
        context.RejectionReasonOptions.Add(reason);
        await context.SaveChangesAsync();
        context.RequestNotes.Add(new RequestNote { RequestId = request.Id, UserId = owner.Id, ReasonId = reason.Id, NoteText = "أول ملاحظة", CreatedDate = DateTime.UtcNow.AddMinutes(-5) });
        context.RequestNotes.Add(new RequestNote { RequestId = request.Id, UserId = owner.Id, ReasonId = reason.Id, NoteText = "آخر ملاحظة", CreatedDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(owner.Id, UserRole.ContractorConsultant, new DashboardSummaryQuery { Period = "all" });

        var item = Assert.Single(summary.ActionItems.Items);
        Assert.Equal("آخر ملاحظة", item.LatestNote);
        Assert.False(item.IsDraft);
    }

    [Fact]
    public async Task Activity_FlattensAndSortsHistoryAcrossRequestsNewestFirst()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var r1 = MakeRequest(owner, company, project, "REQ-1", RequestStatus.Submitted, DateTime.UtcNow);
        var r2 = MakeRequest(owner, company, project, "REQ-2", RequestStatus.Submitted, DateTime.UtcNow);
        context.Requests.AddRange(r1, r2);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        context.RequestHistories.Add(new RequestHistory { RequestId = r1.Id, EventLabel = "إرسال الطلب", EventDate = now.AddDays(-2) });
        context.RequestHistories.Add(new RequestHistory { RequestId = r2.Id, EventLabel = "إرسال الطلب", EventDate = now.AddDays(-1) });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(owner.Id, UserRole.ContractorConsultant, new DashboardSummaryQuery { Period = "all", ActivityPageSize = 10 });

        Assert.Equal(2, summary.Activity.TotalCount);
        Assert.Equal("REQ-2", summary.Activity.Items[0].RequestId);
    }


    [Fact]
    public async Task CompanyBreakdown_ExcludesDrafts()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co A", "Proj");
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-DRAFT", RequestStatus.Draft, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-SUBMITTED", RequestStatus.Submitted, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(owner.Id, UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all" });

        var row = Assert.Single(summary.CompanyBreakdown.Items);
        Assert.Equal(1, row.Total);
    }

    [Fact]
    public async Task CompanyBreakdown_TotalCountCountsGroupsNotRows()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        for (var i = 0; i < 5; i++)
        {
            var (owner, company, project) = SeedApplicant(context, $"Owner {i}", $"Co {i}", "Proj");
            context.Requests.Add(MakeRequest(owner, company, project, $"REQ-{i}-A", RequestStatus.Submitted, DateTime.UtcNow));
            context.Requests.Add(MakeRequest(owner, company, project, $"REQ-{i}-B", RequestStatus.Submitted, DateTime.UtcNow));
        }
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(Guid.NewGuid(), UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all", CompanyPageSize = 2 });

        Assert.Equal(5, summary.CompanyBreakdown.TotalCount);
        Assert.Equal(2, summary.CompanyBreakdown.Items.Count);
    }


    [Fact]
    public async Task ReasonBreakdown_BucketsMissingReasonToOther()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-1", RequestStatus.Rejected, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(owner.Id, UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all" });

        var row = Assert.Single(summary.ReasonBreakdown.Items);
        Assert.Equal("أخرى", row.Reason);
        Assert.Equal(1, row.Count);
    }

    [Fact]
    public async Task ReasonBreakdown_OnlyCountsReturnedOrRejected()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-SUBMITTED", RequestStatus.Submitted, DateTime.UtcNow));
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-REJECTED", RequestStatus.Rejected, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(owner.Id, UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all" });

        Assert.Equal(1, summary.ReasonBreakdown.Items.Sum(r => r.Count));
    }

    [Fact]
    public async Task TotalFlaggedCount_ReflectsWholeSetNotJustCurrentPage()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var reason1 = new RejectionReasonOption { Label = "سبب 1" };
        var reason2 = new RejectionReasonOption { Label = "سبب 2" };
        context.RejectionReasonOptions.AddRange(reason1, reason2);
        for (var i = 0; i < 3; i++)
        {
            context.Requests.Add(MakeRequest(owner, company, project, $"REQ-R1-{i}", RequestStatus.Rejected, DateTime.UtcNow));
        }
        for (var i = 0; i < 2; i++)
        {
            context.Requests.Add(MakeRequest(owner, company, project, $"REQ-R2-{i}", RequestStatus.ReturnedForCorrection, DateTime.UtcNow));
        }
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetReportsSummaryAsync(
            owner.Id, UserRole.P3OOfficer, new ReportsSummaryQuery { Period = "all", ReasonPageSize = 1 });

        Assert.Equal(5, summary.TotalFlaggedCount);
    }


    [Fact]
    public async Task GetActivityPageAsync_ScopesToOwnRequestsAndPages()
    {
        var context = NewContext();
        var (ownerA, companyA, projectA) = SeedApplicant(context, "Owner A", "Co A", "Proj A");
        var (ownerB, companyB, projectB) = SeedApplicant(context, "Owner B", "Co B", "Proj B");
        var reqA = MakeRequest(ownerA, companyA, projectA, "REQ-A", RequestStatus.Submitted, DateTime.UtcNow);
        var reqB = MakeRequest(ownerB, companyB, projectB, "REQ-B", RequestStatus.Submitted, DateTime.UtcNow);
        context.Requests.AddRange(reqA, reqB);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        for (var i = 0; i < 3; i++)
        {
            context.RequestHistories.Add(new RequestHistory { RequestId = reqA.Id, EventLabel = "إرسال الطلب", EventDate = now.AddMinutes(-i) });
        }
        context.RequestHistories.Add(new RequestHistory { RequestId = reqB.Id, EventLabel = "إرسال الطلب", EventDate = now });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var page = await service.GetActivityPageAsync(ownerA.Id, UserRole.ContractorConsultant, page: 1, pageSize: 2);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, i => Assert.Equal("REQ-A", i.RequestId));
    }


    [Fact]
    public async Task GetActionItemsPageAsync_ScopesToOwnRequestsAndPages()
    {
        var context = NewContext();
        var (ownerA, companyA, projectA) = SeedApplicant(context, "Owner A", "Co A", "Proj A");
        var (ownerB, companyB, projectB) = SeedApplicant(context, "Owner B", "Co B", "Proj B");
        for (var i = 0; i < 3; i++)
        {
            context.Requests.Add(MakeRequest(ownerA, companyA, projectA, $"REQ-A-{i}", RequestStatus.Draft, DateTime.UtcNow.AddMinutes(i)));
        }
        context.Requests.Add(MakeRequest(ownerB, companyB, projectB, "REQ-B", RequestStatus.Draft, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var page = await service.GetActionItemsPageAsync(ownerA.Id, UserRole.ContractorConsultant, page: 1, pageSize: 2);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task GetLicensesPageAsync_ScopesToOwnRequestsAndPages()
    {
        var context = NewContext();
        var (ownerA, companyA, projectA) = SeedApplicant(context, "Owner A", "Co A", "Proj A");
        var (ownerB, companyB, projectB) = SeedApplicant(context, "Owner B", "Co B", "Proj B");
        var reqA1 = MakeRequest(ownerA, companyA, projectA, "REQ-A-1", RequestStatus.Activated, DateTime.UtcNow);
        var reqA2 = MakeRequest(ownerA, companyA, projectA, "REQ-A-2", RequestStatus.Activated, DateTime.UtcNow);
        var reqB = MakeRequest(ownerB, companyB, projectB, "REQ-B", RequestStatus.Activated, DateTime.UtcNow);
        context.Requests.AddRange(reqA1, reqA2, reqB);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        context.Licenses.Add(new License { RequestId = reqA1.Id, ActivationDate = now, StartDate = now, EndDate = now.AddMonths(12), Status = LicenseStatus.Active });
        context.Licenses.Add(new License { RequestId = reqA2.Id, ActivationDate = now, StartDate = now, EndDate = now.AddMonths(12), Status = LicenseStatus.Active });
        context.Licenses.Add(new License { RequestId = reqB.Id, ActivationDate = now, StartDate = now, EndDate = now.AddMonths(12), Status = LicenseStatus.Active });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var page = await service.GetLicensesPageAsync(ownerA.Id, UserRole.ContractorConsultant, page: 1, pageSize: 1);

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
    }


    [Fact]
    public async Task Licenses_ExpiryDateReadsRealLicenseEndDate_NotHardcodedTwelveMonths()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var request = MakeRequest(owner, company, project, "REQ-1", RequestStatus.Activated, DateTime.UtcNow);
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var activationDate = DateTime.UtcNow.AddDays(-10);
        var expectedEnd = activationDate.AddMonths(6);
        context.Licenses.Add(new License { RequestId = request.Id, ActivationDate = activationDate, StartDate = activationDate, EndDate = expectedEnd, Status = LicenseStatus.Active });
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(owner.Id, UserRole.ContractorConsultant, new DashboardSummaryQuery { Period = "all" });

        var item = Assert.Single(summary.Licenses.Items);
        Assert.Equal(expectedEnd, item.ExpiryDate);
    }


    [Fact]
    public async Task Stats_PeriodFilterAnchorsToLatestRequestDateInRoleScopedSet()
    {
        var context = NewContext();
        await SeedSystemSettingsAsync(context);
        var (owner, company, project) = SeedApplicant(context, "Owner", "Co", "Proj");
        var latest = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-OLD", RequestStatus.Submitted, latest.AddMonths(-2)));
        context.Requests.Add(MakeRequest(owner, company, project, "REQ-LATEST", RequestStatus.Submitted, latest));
        await context.SaveChangesAsync();

        var service = new RequestAggregateQueryService(context);
        var summary = await service.GetDashboardSummaryAsync(owner.Id, UserRole.ContractorConsultant, new DashboardSummaryQuery { Period = "month" });

        Assert.Equal(1, summary.Stats.Total);
    }
}
