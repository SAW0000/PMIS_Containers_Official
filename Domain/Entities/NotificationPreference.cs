namespace PMIS.Domain.Entities;

public class NotificationPreference : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string PreferenceKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
}
