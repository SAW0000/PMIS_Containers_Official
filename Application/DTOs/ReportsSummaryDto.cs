namespace PMIS.Application.DTOs;

public class ReportsSummaryDto
{
    public ReportStatsDto Stats { get; set; } = new();
    public StatusBarsDto StatusBars { get; set; } = new();
    public DonutsDto Donuts { get; set; } = new();

    public IReadOnlyList<TrendPointDto> Trend { get; set; } = [];

    public SlaDto Sla { get; set; } = new();
    public PagedListDto<CompanyBreakdownRowDto> CompanyBreakdown { get; set; } = new();
    public PagedListDto<ReasonBreakdownRowDto> ReasonBreakdown { get; set; } = new();

    public int TotalFlaggedCount { get; set; }
}

public class ReportStatsDto
{
    public int New { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }
    public int Returned { get; set; }
}

public class TrendPointDto
{
    public string MonthKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public double? AvgDays { get; set; }
}

public class SlaDto
{
    public int TargetDays { get; set; }
    public int WithinCount { get; set; }
    public int TotalRespondedCount { get; set; }
    public int Pct { get; set; }
}

public class CompanyBreakdownRowDto
{
    public string Company { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Activated { get; set; }
    public int Rejected { get; set; }
}

public class ReasonBreakdownRowDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}
