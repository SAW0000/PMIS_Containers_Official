namespace PMIS.Application.DTOs;

public class PagedAdminUsersDto
{
    public IReadOnlyList<AdminUserListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
