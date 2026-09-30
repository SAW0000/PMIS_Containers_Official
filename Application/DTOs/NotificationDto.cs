namespace PMIS.Application.DTOs;

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public bool Unread { get; set; }
}
