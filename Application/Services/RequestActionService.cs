using Microsoft.EntityFrameworkCore;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Application.Services;

public class RequestActionService(
    PmisDbContext context, IUnitOfWork unitOfWork, INotificationService notificationService,
    IAuditLogRepository auditLogRepository)
    : IRequestActionService
{
    private const string RequestNotFoundError = "الطلب غير موجود";

    public async Task<string?> ApproveAsync(string requestNumber, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var request = await FindByNumberAsync(requestNumber, cancellationToken);
        if (request is null) return RequestNotFoundError;
        if (request.Status != RequestStatus.Submitted) return "لا يمكن اعتماد هذا الطلب في حالته الحالية";

        request.Status = RequestStatus.AwaitingPaymentConfirmation;
        request.ReviewDate = DateTime.UtcNow;
        request.ReviewedById = actor.UserId;

        await AddHistoryAsync(request.Id, "قبول الطلب — بانتظار السداد", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(request, actor, "RequestApproved", "Submitted", "AwaitingPaymentConfirmation", cancellationToken);

        await notificationService.NotifyRequestApprovedAsync(request, cancellationToken);
        return null;
    }

    public async Task<string?> ReturnAsync(string requestNumber, RequestActor actor, string note, Guid reasonId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(note)) return "الملاحظة مطلوبة";
        if (await IsUnknownReasonAsync(reasonId, cancellationToken)) return "سبب الإعادة غير صحيح";

        var request = await FindByNumberAsync(requestNumber, cancellationToken);
        if (request is null) return RequestNotFoundError;
        if (request.Status != RequestStatus.Submitted) return "لا يمكن إعادة هذا الطلب في حالته الحالية";

        request.Status = RequestStatus.ReturnedForCorrection;
        request.ReviewDate = DateTime.UtcNow;
        request.ReviewedById = actor.UserId;

        await unitOfWork.RequestNotes.AddAsync(new RequestNote
        {
            RequestId = request.Id,
            UserId = actor.UserId,
            NoteText = note.Trim(),
            NoteType = RequestNoteType.Return,
            ReasonId = reasonId
        }, cancellationToken);

        await AddHistoryAsync(request.Id, "إعادة الطلب للمستفيد", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(request, actor, "RequestReturned", "Submitted", "ReturnedForCorrection", cancellationToken);

        await notificationService.NotifyRequestReturnedAsync(request, cancellationToken);
        return null;
    }

    public async Task<string?> RejectAsync(string requestNumber, RequestActor actor, string note, Guid reasonId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(note)) return "الملاحظة مطلوبة";
        if (await IsUnknownReasonAsync(reasonId, cancellationToken)) return "سبب الرفض غير صحيح";

        var request = await FindByNumberAsync(requestNumber, cancellationToken);
        if (request is null) return RequestNotFoundError;
        if (request.Status != RequestStatus.Submitted) return "لا يمكن رفض هذا الطلب في حالته الحالية";

        request.Status = RequestStatus.Rejected;
        request.ReviewDate = DateTime.UtcNow;
        request.ReviewedById = actor.UserId;

        await unitOfWork.RequestNotes.AddAsync(new RequestNote
        {
            RequestId = request.Id,
            UserId = actor.UserId,
            NoteText = note.Trim(),
            NoteType = RequestNoteType.Rejection,
            ReasonId = reasonId
        }, cancellationToken);

        await AddHistoryAsync(request.Id, "رفض الطلب", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(request, actor, "RequestRejected", "Submitted", "Rejected", cancellationToken);

        await notificationService.NotifyRequestRejectedAsync(request, cancellationToken);
        return null;
    }

    public async Task<string?> ConfirmPaymentAsync(string requestNumber, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var request = await FindByNumberAsync(requestNumber, cancellationToken);
        if (request is null) return RequestNotFoundError;
        if (request.Status != RequestStatus.AwaitingPaymentConfirmation) return "لا يمكن تأكيد السداد لهذا الطلب في حالته الحالية";

        request.Status = RequestStatus.PaymentConfirmed;
        request.PaymentConfirmedDate = DateTime.UtcNow;

        await AddHistoryAsync(request.Id, "تأكيد السداد", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(request, actor, "PaymentConfirmed", "AwaitingPaymentConfirmation", "PaymentConfirmed", cancellationToken);

        await notificationService.NotifyPaymentConfirmedAsync(request, cancellationToken);
        return null;
    }

    public async Task<string?> ActivateAsync(string requestNumber, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var request = await FindByNumberAsync(requestNumber, cancellationToken);
        if (request is null) return RequestNotFoundError;
        if (request.Status != RequestStatus.PaymentConfirmed) return "لا يمكن تفعيل هذا الطلب في حالته الحالية";

        var settings = await unitOfWork.SystemSettings.GetByIdAsync(SystemSettingsConfiguration.SingletonId, cancellationToken)
            ?? throw new InvalidOperationException("SystemSettings singleton row is missing — check the InitialCreate migration's seed data.");

        var activationDate = DateTime.UtcNow;
        request.Status = RequestStatus.Activated;

        if (request.License is null)
        {
            await unitOfWork.Licenses.AddAsync(new License
            {
                RequestId = request.Id,
                ActivationDate = activationDate,
                StartDate = activationDate,
                EndDate = activationDate.AddMonths(settings.LicenseValidityMonths),
                Status = LicenseStatus.Active
            }, cancellationToken);
        }
        else
        {
            request.License.ActivationDate = activationDate;
            request.License.StartDate = activationDate;
            request.License.EndDate = activationDate.AddMonths(settings.LicenseValidityMonths);
            request.License.Status = LicenseStatus.Active;
        }

        await AddHistoryAsync(request.Id, "تفعيل الرخصة", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(request, actor, "LicenseActivated", "PaymentConfirmed", "Activated", cancellationToken);

        await notificationService.NotifyRequestActivatedAsync(request, cancellationToken);
        return null;
    }


    private Task WriteAuditAsync(
        Request request, RequestActor actor, string action, string fromState, string toState,
        CancellationToken cancellationToken)
        => auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Request",
            EntityId = request.Id,
            Action = action,
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            FromState = fromState,
            ToState = toState,
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

    private async Task<bool> IsUnknownReasonAsync(Guid reasonId, CancellationToken cancellationToken)
        => !await context.RejectionReasonOptions.AnyAsync(o => o.Id == reasonId && o.IsActive, cancellationToken);

    private Task<Request?> FindByNumberAsync(string requestNumber, CancellationToken cancellationToken)
        => context.Requests
            .Include(r => r.License)
            .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber, cancellationToken);

    private Task AddHistoryAsync(Guid requestId, string eventLabel, CancellationToken cancellationToken)
        => unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            RequestId = requestId,
            EventLabel = eventLabel,
            EventDate = DateTime.UtcNow
        }, cancellationToken);
}
