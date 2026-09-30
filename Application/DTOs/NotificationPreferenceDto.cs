namespace PMIS.Application.DTOs;

public class NotificationPreferenceDto
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class NotificationPreferenceUpdateDto
{
    public List<NotificationPreferenceDto> Preferences { get; set; } = [];
}
