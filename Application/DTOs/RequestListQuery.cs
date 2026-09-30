namespace PMIS.Application.DTOs;

public class RequestListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public List<string> Statuses { get; set; } = [];

    public string Period { get; set; } = "all";

    public string SortCol { get; set; } = "date";

    public string SortDir { get; set; } = "desc";
}
