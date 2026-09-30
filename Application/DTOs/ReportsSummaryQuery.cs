namespace PMIS.Application.DTOs;

public class ReportsSummaryQuery
{
    public string Period { get; set; } = "month";

    public int CompanyPage { get; set; } = 1;
    public int CompanyPageSize { get; set; } = 3;

    public int ReasonPage { get; set; } = 1;
    public int ReasonPageSize { get; set; } = 3;
}
