using PMIS.Domain.Entities;

namespace PMIS.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task AddManyAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<long> GetTotalCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<long> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(string notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<long> DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<long> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default);

    Task<long> DeleteExcessPerUserAsync(int keepCountPerUser, CancellationToken cancellationToken = default);
}
