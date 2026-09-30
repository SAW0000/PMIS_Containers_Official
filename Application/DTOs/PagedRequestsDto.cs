namespace PMIS.Application.DTOs;

public class PagedRequestsDto
{
    public IReadOnlyList<RequestListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
