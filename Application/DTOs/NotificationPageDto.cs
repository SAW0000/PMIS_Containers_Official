namespace PMIS.Application.DTOs;

public class NotificationPageDto
{
    public IReadOnlyList<NotificationDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
