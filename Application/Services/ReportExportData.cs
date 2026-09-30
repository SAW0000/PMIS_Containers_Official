using PMIS.Application.DTOs;

namespace PMIS.Application.Services;

public record ReportExportData(
    ReportPeriod Period,
    IReadOnlyList<RequestListItemDto> Requests,
    int SlaTargetDays,
    int SlaWithinCount,
    int SlaTotalRespondedCount,
    IReadOnlyList<CompanyBreakdownRow> CompanyBreakdown,
    IReadOnlyList<ReasonBreakdownRow> ReasonBreakdown)
{
    public int SlaPercent => SlaTotalRespondedCount == 0 ? 0 : (int)Math.Round(SlaWithinCount * 100.0 / SlaTotalRespondedCount);
}

public record CompanyBreakdownRow(string Company, int Total, int Activated, int Rejected);

public record ReasonBreakdownRow(string LabelArabic, int Count);

public static class ReportStatusLabels
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["draft"] = "مسودة",
        ["submitted"] = "قيد المراجعة",
        ["returned"] = "معاد للمستفيد",
        ["rejected"] = "مرفوض",
        ["awaiting_payment"] = "بانتظار تأكيد السداد",
        ["approved"] = "معتمد",
        ["activated"] = "تم التفعيل",
        ["cancelled"] = "ملغى"
    };

    public static string For(string statusKey) => Labels.GetValueOrDefault(statusKey, statusKey);

    public static readonly IReadOnlyDictionary<string, string> PeriodLabels = new Dictionary<string, string>
    {
        ["Today"] = "اليوم",
        ["Week"] = "هذا الأسبوع",
        ["Month"] = "هذا الشهر",
        ["Year"] = "هذا السنة",
        ["All"] = "كل الأوقات"
    };
}
