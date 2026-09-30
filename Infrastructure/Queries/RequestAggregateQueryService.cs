using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Infrastructure.Queries;

public class RequestAggregateQueryService(PmisDbContext context) : IRequestAggregateQueryService
{
    private static readonly string[] ResolvingEventLabels = ["رفض الطلب", "اعتماد الطلب", "تفعيل الرخصة"];
    private static readonly string[] FirstResponseEventLabels =
        ["مراجعة مكتب P3O", "رفض الطلب", "إعادة الطلب للمستفيد", "قبول الطلب — بانتظار السداد", "اعتماد الطلب"];

    private const string SubmittedEventLabel = "إرسال الطلب";

    private static readonly RequestStatus[] StatusOrder =
    [
        RequestStatus.Draft, RequestStatus.Submitted, RequestStatus.ReturnedForCorrection, RequestStatus.Rejected,
        RequestStatus.AwaitingPaymentConfirmation, RequestStatus.PaymentConfirmed, RequestStatus.Activated, RequestStatus.Cancelled
    ];

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        Guid currentUserId, UserRole currentRole, DashboardSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var scoped = ApplyRoleScope(context.Requests.AsNoTracking().Where(r => r.IsActive), currentUserId, currentRole);

        return new DashboardSummaryDto
        {
            Stats = await BuildDashboardStatsAsync(scoped, query.Period, cancellationToken),
            ActionItems = await BuildActionItemsPageAsync(scoped, query.ActionPage, query.ActionPageSize, cancellationToken),
            Licenses = await BuildLicensesPageAsync(scoped, query.LicensesPage, query.LicensesPageSize, cancellationToken),
            Activity = await BuildActivityPageAsync(scoped, query.ActivityPage, query.ActivityPageSize, cancellationToken),
            StatusBars = await BuildStatusBarsAsync(scoped, cancellationToken),
            Donuts = await BuildDonutsAsync(scoped, cancellationToken)
        };
    }

    public async Task<ReportsSummaryDto> GetReportsSummaryAsync(
        Guid currentUserId, UserRole currentRole, ReportsSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var scoped = ApplyRoleScope(context.Requests.AsNoTracking().Where(r => r.IsActive), currentUserId, currentRole);

        var (within, totalResponded, slaTargetDays) = await BuildSlaAsync(scoped, cancellationToken);
        var pct = totalResponded > 0 ? (int)Math.Round(within / (double)totalResponded * 100) : 0;

        return new ReportsSummaryDto
        {
            Stats = await BuildReportStatsAsync(scoped, query.Period, cancellationToken),
            StatusBars = await BuildStatusBarsAsync(scoped, cancellationToken),
            Donuts = await BuildDonutsAsync(scoped, cancellationToken),
            Trend = await BuildTrendAsync(scoped, cancellationToken),
            Sla = new SlaDto { TargetDays = slaTargetDays, WithinCount = within, TotalRespondedCount = totalResponded, Pct = pct },
            CompanyBreakdown = await BuildCompanyBreakdownPageAsync(scoped, query.CompanyPage, query.CompanyPageSize, cancellationToken),
            ReasonBreakdown = await BuildReasonBreakdownPageAsync(scoped, query.ReasonPage, query.ReasonPageSize, cancellationToken),
            TotalFlaggedCount = await scoped.CountAsync(
                r => r.Status == RequestStatus.ReturnedForCorrection || r.Status == RequestStatus.Rejected, cancellationToken)
        };
    }

    public Task<PagedListDto<ActivityEventDto>> GetActivityPageAsync(
        Guid currentUserId, UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var scoped = ApplyRoleScope(context.Requests.AsNoTracking().Where(r => r.IsActive), currentUserId, currentRole);
        return BuildActivityPageAsync(scoped, page, pageSize, cancellationToken);
    }

    public Task<PagedListDto<ActionItemDto>> GetActionItemsPageAsync(
        Guid currentUserId, UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var scoped = ApplyRoleScope(context.Requests.AsNoTracking().Where(r => r.IsActive), currentUserId, currentRole);
        return BuildActionItemsPageAsync(scoped, page, pageSize, cancellationToken);
    }

    public Task<PagedListDto<LicenseItemDto>> GetLicensesPageAsync(
        Guid currentUserId, UserRole currentRole, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var scoped = ApplyRoleScope(context.Requests.AsNoTracking().Where(r => r.IsActive), currentUserId, currentRole);
        return BuildLicensesPageAsync(scoped, page, pageSize, cancellationToken);
    }

    private static IQueryable<Request> ApplyRoleScope(IQueryable<Request> query, Guid currentUserId, UserRole currentRole)
        => currentRole == UserRole.ContractorConsultant ? query.Where(r => r.UserId == currentUserId) : query;

    private static async Task<IQueryable<Request>> ApplyPeriodAsync(IQueryable<Request> scoped, string? period, CancellationToken cancellationToken)
    {
        if (period is null or "all")
        {
            return scoped;
        }

        var referenceDate = await RequestQueryService.LatestRequestDateAsync(scoped, cancellationToken);
        return RequestQueryService.ApplyPeriodFilter(scoped, period, referenceDate);
    }

    private static async Task<DashboardStatsDto> BuildDashboardStatsAsync(IQueryable<Request> scoped, string? period, CancellationToken cancellationToken)
    {
        var periodScoped = await ApplyPeriodAsync(scoped, period, cancellationToken);

        return new DashboardStatsDto
        {
            Total = await periodScoped.CountAsync(cancellationToken),
            Submitted = await periodScoped.CountAsync(r => r.Status == RequestStatus.Submitted, cancellationToken),
            Activated = await periodScoped.CountAsync(r => r.Status == RequestStatus.Activated, cancellationToken),
            NeedsAction = await periodScoped.CountAsync(
                r => r.Status == RequestStatus.Draft || r.Status == RequestStatus.ReturnedForCorrection, cancellationToken)
        };
    }

    private static async Task<ReportStatsDto> BuildReportStatsAsync(IQueryable<Request> scoped, string? period, CancellationToken cancellationToken)
    {
        var periodScoped = await ApplyPeriodAsync(scoped, period, cancellationToken);

        return new ReportStatsDto
        {
            New = await periodScoped.CountAsync(r => r.Status == RequestStatus.Submitted, cancellationToken),
            Accepted = await periodScoped.CountAsync(r =>
                r.Status == RequestStatus.PaymentConfirmed || r.Status == RequestStatus.Activated || r.Status == RequestStatus.AwaitingPaymentConfirmation,
                cancellationToken),
            Rejected = await periodScoped.CountAsync(r => r.Status == RequestStatus.Rejected, cancellationToken),
            Returned = await periodScoped.CountAsync(r => r.Status == RequestStatus.ReturnedForCorrection, cancellationToken)
        };
    }

    private static async Task<StatusBarsDto> BuildStatusBarsAsync(IQueryable<Request> scoped, CancellationToken cancellationToken)
    {
        var counts = await scoped
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = counts.ToDictionary(c => c.Status, c => c.Count);

        return new StatusBarsDto
        {
            Counts = StatusOrder
                .Select(status => new StatusCountDto { StatusKey = MockStatusKey(status), Count = byStatus.GetValueOrDefault(status) })
                .ToList()
        };
    }

    private static async Task<DonutsDto> BuildDonutsAsync(IQueryable<Request> scoped, CancellationToken cancellationToken)
    {
        var total = await scoped.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new DonutsDto();
        }

        var activated = await scoped.CountAsync(r => r.Status == RequestStatus.Activated, cancellationToken);
        var rejected = await scoped.CountAsync(r => r.Status == RequestStatus.Rejected, cancellationToken);
        var returned = await scoped.CountAsync(r => r.Status == RequestStatus.ReturnedForCorrection, cancellationToken);

        return new DonutsDto
        {
            ActivatedPct = (int)Math.Round(activated / (double)total * 100),
            AcceptedPct = (int)Math.Round((total - rejected) / (double)total * 100),
            ReturnedPct = (int)Math.Round(returned / (double)total * 100)
        };
    }

    private static async Task<PagedListDto<ActionItemDto>> BuildActionItemsPageAsync(
        IQueryable<Request> scoped, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var actionQuery = scoped
            .Where(r => r.Status == RequestStatus.Draft || r.Status == RequestStatus.ReturnedForCorrection)
            .OrderByDescending(r => r.RequestDate);

        var total = await actionQuery.CountAsync(cancellationToken);
        var items = await actionQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ActionItemDto
            {
                Id = r.RequestNumber,
                RequestId = r.Id,
                Project = r.Project.ProjectName ?? string.Empty,
                Status = r.Status == RequestStatus.Draft ? "draft" : "returned",
                IsDraft = r.Status == RequestStatus.Draft,
                LatestNote = r.RequestNotes
                    .OrderByDescending(n => n.CreatedDate)
                    .Select(n => n.NoteText)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new PagedListDto<ActionItemDto> { Items = items, TotalCount = total };
    }

    private static async Task<PagedListDto<LicenseItemDto>> BuildLicensesPageAsync(
        IQueryable<Request> scoped, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var licensesQuery = scoped
            .Where(r => r.Status == RequestStatus.Activated && r.License != null)
            .OrderByDescending(r => r.License!.ActivationDate);

        var total = await licensesQuery.CountAsync(cancellationToken);
        var rows = await licensesQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { r.RequestNumber, r.License!.ActivationDate, r.License!.EndDate })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow.Date;
        var items = rows.Select(r => new LicenseItemDto
        {
            Id = r.RequestNumber,
            ActivationDate = r.ActivationDate ?? now,
            ExpiryDate = r.EndDate ?? now,
            DaysLeft = (int)Math.Ceiling(((r.EndDate ?? now) - now).TotalDays)
        }).ToList();

        return new PagedListDto<LicenseItemDto> { Items = items, TotalCount = total };
    }

    private static async Task<PagedListDto<ActivityEventDto>> BuildActivityPageAsync(
        IQueryable<Request> scoped, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var activityQuery = scoped
            .SelectMany(r => r.RequestHistories.Select(h => new { r.RequestNumber, h.EventLabel, h.EventDate }))
            .OrderByDescending(x => x.EventDate);

        var total = await activityQuery.CountAsync(cancellationToken);
        var items = await activityQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ActivityEventDto { Label = x.EventLabel, Date = x.EventDate, RequestId = x.RequestNumber })
            .ToListAsync(cancellationToken);

        return new PagedListDto<ActivityEventDto> { Items = items, TotalCount = total };
    }

    private static async Task<List<ResolutionRow>> BuildResolutionRowsAsync(IQueryable<Request> scoped, CancellationToken cancellationToken)
    {
        var raw = await scoped
            .Select(r => new
            {
                r.RequestDate,
                SubmittedDate = r.RequestHistories
                    .Where(h => h.EventLabel == SubmittedEventLabel)
                    .OrderBy(h => h.EventDate)
                    .Select(h => (DateTime?)h.EventDate)
                    .FirstOrDefault(),
                ResolvedDate = r.RequestHistories
                    .Where(h => ResolvingEventLabels.Contains(h.EventLabel))
                    .OrderBy(h => h.EventDate)
                    .Select(h => (DateTime?)h.EventDate)
                    .FirstOrDefault(),
                RespondedDate = r.RequestHistories
                    .Where(h => FirstResponseEventLabels.Contains(h.EventLabel))
                    .OrderBy(h => h.EventDate)
                    .Select(h => (DateTime?)h.EventDate)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return raw
            .Where(r => r.SubmittedDate is not null)
            .Select(r => new ResolutionRow(
                SubmittedDate: r.SubmittedDate!.Value,
                ResolutionDays: r.ResolvedDate is null ? null : Math.Max(0, (int)Math.Round((r.ResolvedDate.Value - r.SubmittedDate.Value).TotalDays)),
                ResponseDays: r.RespondedDate is null ? null : Math.Max(0, (int)Math.Round((r.RespondedDate.Value - r.SubmittedDate.Value).TotalDays))))
            .ToList();
    }

    private sealed record ResolutionRow(DateTime SubmittedDate, int? ResolutionDays, int? ResponseDays);

    private async Task<(int Within, int Total, int TargetDays)> BuildSlaAsync(IQueryable<Request> scoped, CancellationToken cancellationToken)
    {
        var slaTargetDays = await context.SystemSettings
            .Where(s => s.Id == SystemSettingsConfiguration.SingletonId)
            .Select(s => s.ResponseSlaBusinessDays)
            .FirstOrDefaultAsync(cancellationToken);

        var rows = await BuildResolutionRowsAsync(scoped, cancellationToken);
        var responded = rows.Where(r => r.ResponseDays is not null).ToList();
        var within = responded.Count(r => r.ResponseDays!.Value <= slaTargetDays);

        return (within, responded.Count, slaTargetDays);
    }

    private static readonly string[] MonthLabels =
        ["ينا", "فبر", "مار", "أبر", "ماي", "يون", "يول", "أغس", "سبت", "أكت", "نوف", "ديس"];

    private static async Task<List<TrendPointDto>> BuildTrendAsync(IQueryable<Request> scoped, CancellationToken cancellationToken)
    {
        var rows = await BuildResolutionRowsAsync(scoped, cancellationToken);
        var latest = rows.Count > 0 ? rows.Max(r => r.SubmittedDate) : DateTime.UtcNow;

        var months = Enumerable.Range(0, 6)
            .Select(i => new DateTime(latest.Year, latest.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(5 - i)))
            .ToList();

        return months.Select(month =>
        {
            var inMonth = rows.Where(r => r.SubmittedDate.Year == month.Year && r.SubmittedDate.Month == month.Month && r.ResolutionDays is not null)
                .Select(r => r.ResolutionDays!.Value)
                .ToList();

            return new TrendPointDto
            {
                MonthKey = $"{month.Year}-{month.Month:D2}",
                Label = MonthLabels[month.Month - 1],
                AvgDays = inMonth.Count > 0 ? Math.Round(inMonth.Average(), 1) : null
            };
        }).ToList();
    }

    private static async Task<PagedListDto<CompanyBreakdownRowDto>> BuildCompanyBreakdownPageAsync(
        IQueryable<Request> scoped, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var companyQuery = scoped
            .Where(r => r.Status != RequestStatus.Draft)
            .GroupBy(r => r.Company.CompanyName)
            .Select(g => new CompanyBreakdownRowDto
            {
                Company = g.Key ?? string.Empty,
                Total = g.Count(),
                Activated = g.Count(r => r.Status == RequestStatus.Activated),
                Rejected = g.Count(r => r.Status == RequestStatus.Rejected)
            })
            .OrderByDescending(row => row.Total);

        var total = await companyQuery.CountAsync(cancellationToken);
        var items = await companyQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedListDto<CompanyBreakdownRowDto> { Items = items, TotalCount = total };
    }

    private static async Task<PagedListDto<ReasonBreakdownRowDto>> BuildReasonBreakdownPageAsync(
        IQueryable<Request> scoped, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var reasonQuery = scoped
            .Where(r => r.Status == RequestStatus.ReturnedForCorrection || r.Status == RequestStatus.Rejected)
            .Select(r => r.RequestNotes
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => n.Reason.Label)
                .FirstOrDefault() ?? "أخرى")
            .GroupBy(label => label)
            .Select(g => new ReasonBreakdownRowDto { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(row => row.Count);

        var total = await reasonQuery.CountAsync(cancellationToken);
        var items = await reasonQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedListDto<ReasonBreakdownRowDto> { Items = items, TotalCount = total };
    }

    private static string MockStatusKey(RequestStatus status) => status switch
    {
        RequestStatus.Draft => "draft",
        RequestStatus.Submitted => "submitted",
        RequestStatus.ReturnedForCorrection => "returned",
        RequestStatus.Rejected => "rejected",
        RequestStatus.AwaitingPaymentConfirmation => "awaiting_payment",
        RequestStatus.PaymentConfirmed => "approved",
        RequestStatus.Activated => "activated",
        RequestStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped request status")
    };
}
