using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface INotificationQueryService
{
    Task<NotificationPageDto> GetPagedForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetUnreadForUserAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(string notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
