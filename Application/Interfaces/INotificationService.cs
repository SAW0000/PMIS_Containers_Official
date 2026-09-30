using PMIS.Domain.Entities;

namespace PMIS.Application.Interfaces;

public interface INotificationService
{
    Task NotifyRequestSubmittedAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyRequestResubmittedAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyRequestApprovedAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyRequestReturnedAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyRequestRejectedAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyPaymentConfirmedAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyRequestActivatedAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyRequestCancelledByBeneficiaryAsync(Request request, CancellationToken cancellationToken = default);

    Task NotifyAccountChangedAsync(User affectedUser, Guid actorUserId, CancellationToken cancellationToken = default);

    Task NotifyOwnAccountChangedAsync(User affectedUser, CancellationToken cancellationToken = default);

    Task NotifyNewAuditLogEntryAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task NotifyOverdueRequestsDigestAsync(int count, CancellationToken cancellationToken = default);

    Task NotifyPaymentConfirmationNeededDigestAsync(int count, CancellationToken cancellationToken = default);
}
