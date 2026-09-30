namespace PMIS.Application.DTOs;

public class AdminUserListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public List<string> Roles { get; set; } = [];

    public string Active { get; set; } = "all";

    public string SortCol { get; set; } = "name";

    public string SortDir { get; set; } = "asc";
}
