namespace PMIS.Application.DTOs;

public class PagedListDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
