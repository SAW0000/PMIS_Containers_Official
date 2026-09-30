namespace PMIS.Application.DTOs;

public class DashboardSummaryQuery
{
    public string Period { get; set; } = "month";

    public int ActionPage { get; set; } = 1;
    public int ActionPageSize { get; set; } = 2;

    public int LicensesPage { get; set; } = 1;
    public int LicensesPageSize { get; set; } = 2;

    public int ActivityPage { get; set; } = 1;
    public int ActivityPageSize { get; set; } = 10;
}
