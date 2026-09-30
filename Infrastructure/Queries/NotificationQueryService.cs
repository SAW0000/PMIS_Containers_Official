using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Queries;

public class NotificationQueryService(INotificationRepository repository) : INotificationQueryService
{
    public async Task<NotificationPageDto> GetPagedForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var pageClamped = Math.Max(1, page);
        var pageSizeClamped = Math.Clamp(pageSize, 1, 100);

        var notifications = await repository.GetForUserAsync(userId, pageClamped, pageSizeClamped, cancellationToken);
        var totalCount = await repository.GetTotalCountAsync(userId, cancellationToken);

        return new NotificationPageDto
        {
            Items = notifications.Select(ToDto).ToList(),
            TotalCount = (int)totalCount
        };
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadForUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await repository.GetUnreadForUserAsync(userId, cancellationToken);
        return notifications.Select(ToDto).ToList();
    }

    public async Task<bool> MarkReadAsync(string notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(notificationId, out _))
        {
            return false;
        }

        await repository.MarkReadAsync(notificationId, userId, cancellationToken);
        return true;
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => repository.MarkAllReadAsync(userId, cancellationToken);

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id.ToString(),
        Icon = IconForType(n.Type),
        Title = n.Title,
        Text = n.Message,
        CreatedDate = n.CreatedDate,
        Unread = !n.IsRead
    };

    private static string IconForType(string type) => type switch
    {
        "RequestSubmitted" => "send",
        "ReturnedForCorrection" => "back",
        "Rejected" => "reject",
        "AwaitingPayment" => "checkAll",
        "PaymentConfirmed" => "check",
        "Activated" => "check",
        "NewRequestArrived" => "send",
        "RequestCancelled" => "reject",
        "AccountChanged" => "account",
        "OwnAccountChanged" => "account",
        "NewAuditLogEntry" => "log",
        "OverdueRequestsDigest" => "alert",
        "PaymentConfirmationNeededDigest" => "alert",
        _ => "bell"
    };
}
