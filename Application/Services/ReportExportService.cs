using System.Globalization;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Reporting;

namespace PMIS.Application.Services;

public class ReportExportService(
    IRequestQueryService requestQueryService,
    ISystemSettingsService systemSettingsService,
    IReportExcelBuilder excelBuilder,
    IReportPdfBuilder pdfBuilder,
    ReportGenerationLimiter limiter) : IReportExportService
{
    private static readonly string[] FirstResponseEvents =
        ["مراجعة مكتب P3O", "رفض الطلب", "إعادة الطلب للمستفيد", "قبول الطلب — بانتظار السداد", "اعتماد الطلب"];

    public async Task<ReportExportFile> ExportAsync(
        Guid currentUserId, UserRole currentRole, ReportPeriod period, ReportExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var allRequests = await requestQueryService.GetVisibleRequestsAsync(currentUserId, currentRole, cancellationToken: cancellationToken);
        var filtered = ReportPeriodFilter.Apply(allRequests, period);
        var settings = await systemSettingsService.GetAsync(cancellationToken);

        var data = BuildExportData(period, filtered, settings.ResponseSlaBusinessDays);

        return format == ReportExportFormat.Excel
            ? await limiter.RunAsync(() => excelBuilder.Build(data), cancellationToken)
            : await limiter.RunAsync(() => pdfBuilder.Build(data), cancellationToken);
    }

    private static ReportExportData BuildExportData(ReportPeriod period, IReadOnlyList<RequestListItemDto> requests, int slaTargetDays)
    {
        var (within, totalResponded) = ComputeSlaCounts(requests, slaTargetDays);

        return new ReportExportData(
            period,
            requests,
            slaTargetDays,
            within,
            totalResponded,
            BuildCompanyBreakdown(requests),
            BuildReasonBreakdown(requests));
    }

    private static (int Within, int Total) ComputeSlaCounts(IReadOnlyList<RequestListItemDto> requests, int slaTargetDays)
    {
        var respondedDays = requests
            .Select(FirstResponseDays)
            .Where(d => d is not null)
            .Select(d => d!.Value)
            .ToList();

        var within = respondedDays.Count(d => d <= slaTargetDays);
        return (within, respondedDays.Count);
    }

    private static int? FirstResponseDays(RequestListItemDto request)
    {
        var submitted = request.History.FirstOrDefault(h => h[0] == "إرسال الطلب");
        if (submitted is null || submitted[1] is null)
        {
            return null;
        }

        var responded = request.History.FirstOrDefault(h => h[0] is not null && FirstResponseEvents.Contains(h[0]));
        if (responded is null || responded[1] is null)
        {
            return null;
        }

        return DaysBetween(submitted[1]!, responded[1]!);
    }

    private static int DaysBetween(string startDate, string endDate)
    {
        var start = DateTime.Parse(startDate, CultureInfo.InvariantCulture);
        var end = DateTime.Parse(endDate, CultureInfo.InvariantCulture);
        return Math.Max(0, (int)Math.Round((end - start).TotalDays));
    }

    private static List<CompanyBreakdownRow> BuildCompanyBreakdown(IReadOnlyList<RequestListItemDto> requests)
        => requests
            .GroupBy(r => r.Company)
            .Select(g => new CompanyBreakdownRow(
                g.Key,
                g.Count(),
                g.Count(r => r.Status == "activated"),
                g.Count(r => r.Status == "rejected")))
            .OrderByDescending(row => row.Total)
            .ToList();

    private static List<ReasonBreakdownRow> BuildReasonBreakdown(IReadOnlyList<RequestListItemDto> requests)
        => requests
            .Where(r => r.Status is "returned" or "rejected")
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Reason) ? "أخرى" : r.Reason)
            .Select(g => new ReasonBreakdownRow(g.Key, g.Count()))
            .OrderByDescending(row => row.Count)
            .ToList();
}
