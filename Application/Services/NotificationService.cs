using Microsoft.EntityFrameworkCore;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    INotificationPreferenceService preferenceService,
    PmisDbContext context) : INotificationService
{
    public async Task NotifyRequestSubmittedAsync(Request request, CancellationToken cancellationToken = default)
    {
        await WriteAsync(
            request.UserId,
            NotificationTypes.RequestSubmitted,
            $"تم إرسال الطلب {request.RequestNumber}",
            "تم استلام طلبكم وهو الآن قيد المراجعة من قبل مكتب P3O.",
            request,
            cancellationToken);

        await NotifyOfficersOfNewWorkItemAsync(
            request,
            $"طلب جديد بانتظار المراجعة {request.RequestNumber}",
            $"تم إرسال طلب جديد من {ApplicantNameOf(request)} ويتطلب المراجعة.",
            cancellationToken);
    }

    public Task NotifyRequestResubmittedAsync(Request request, CancellationToken cancellationToken = default)
        => NotifyOfficersOfNewWorkItemAsync(
            request,
            $"تمت إعادة إرسال الطلب {request.RequestNumber}",
            $"أعاد {ApplicantNameOf(request)} إرسال الطلب بعد استكمال الملاحظات، ويتطلب المراجعة.",
            cancellationToken);

    public Task NotifyRequestApprovedAsync(Request request, CancellationToken cancellationToken = default)
        => WriteAsync(
            request.UserId,
            NotificationTypes.AwaitingPayment,
            $"تم قبول الطلب {request.RequestNumber} - بانتظار السداد",
            "تم قبول طلبكم، يرجى إتمام إجراءات السداد لاستكمال التفعيل.",
            request,
            cancellationToken);

    public Task NotifyRequestReturnedAsync(Request request, CancellationToken cancellationToken = default)
        => WriteAsync(
            request.UserId,
            NotificationTypes.ReturnedForCorrection,
            $"تم إعادة الطلب {request.RequestNumber} لاستكمال المستندات",
            "يرجى مراجعة ملاحظات مكتب P3O وإعادة إرسال الطلب.",
            request,
            cancellationToken);

    public Task NotifyRequestRejectedAsync(Request request, CancellationToken cancellationToken = default)
        => WriteAsync(
            request.UserId,
            NotificationTypes.Rejected,
            $"تم رفض الطلب {request.RequestNumber}",
            "نأسف لإبلاغكم برفض الطلب. راجع سبب الرفض في تفاصيل الطلب.",
            request,
            cancellationToken);

    public Task NotifyPaymentConfirmedAsync(Request request, CancellationToken cancellationToken = default)
        => WriteAsync(
            request.UserId,
            NotificationTypes.PaymentConfirmed,
            $"تم تأكيد سداد الطلب {request.RequestNumber}",
            "تم تأكيد استلام السداد، الرخصة الآن قيد التفعيل.",
            request,
            cancellationToken);

    public Task NotifyRequestActivatedAsync(Request request, CancellationToken cancellationToken = default)
        => WriteAsync(
            request.UserId,
            NotificationTypes.Activated,
            $"تم تفعيل الرخصة للطلب {request.RequestNumber}",
            "تهانينا، تم تفعيل رخصتكم بنجاح وهي جاهزة للاستخدام.",
            request,
            cancellationToken);

    public Task NotifyRequestCancelledByBeneficiaryAsync(Request request, CancellationToken cancellationToken = default)
        => NotifyOfficersAsync(
            NotificationTypes.RequestCancelled,
            $"تم إلغاء الطلب {request.RequestNumber}",
            $"ألغى {ApplicantNameOf(request)} الطلب أثناء إجراءات المراجعة.",
            cancellationToken,
            request);

    public async Task NotifyAccountChangedAsync(
        User affectedUser, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var recipientIds = await context.Users
            .Where(u => u.IsActive && !u.DeletedAccount
                && u.RoleAssignments.Any(ra => ra.Role == UserRole.P3OOfficer || ra.Role == UserRole.SystemAdministrator)
                && u.Id != actorUserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        const string title = "تحديث على حسابات المستخدمين";
        var message = $"تم إجراء تغيير على حساب {affectedUser.FullName}.";

        await WriteManyAsync(recipientIds, NotificationTypes.AccountChanged, title, message, request: null, cancellationToken);
    }

    public async Task NotifyOwnAccountChangedAsync(User affectedUser, CancellationToken cancellationToken = default)
    {
        if (!affectedUser.RoleAssignments.Any(ra => ra.Role is UserRole.P3OOfficer or UserRole.SystemAdministrator))
        {
            return;
        }

        await WriteAsync(
            affectedUser.Id,
            NotificationTypes.OwnAccountChanged,
            "تم تحديث بيانات حسابك",
            "تم إجراء تغيير على بيانات حسابك من قبل الإدارة. يرجى مراجعة بياناتك.",
            request: null,
            cancellationToken);
    }

    public async Task NotifyNewAuditLogEntryAsync(
        AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var adminIds = await context.Users
            .Where(u => u.RoleAssignments.Any(ra => ra.Role == UserRole.SystemAdministrator) && u.IsActive && !u.DeletedAccount)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        const string title = "إدخال جديد في سجل العمليات";
        var action = AuditActionLabels.For(entry.Action);
        var actor = string.IsNullOrWhiteSpace(entry.PerformedBy?.FullNameSnapshot)
            ? "مستخدم غير معروف"
            : entry.PerformedBy.FullNameSnapshot;
        var message = entry.RequestNumberSnapshot is { Length: > 0 } requestNumber
            ? $"{action} — {actor} على الطلب {requestNumber}."
            : $"{action} — {actor}.";

        await WriteManyAsync(adminIds, NotificationTypes.NewAuditLogEntry, title, message, request: null, cancellationToken);
    }

    public Task NotifyOverdueRequestsDigestAsync(int count, CancellationToken cancellationToken = default)
        => NotifyOfficersAsync(
            NotificationTypes.OverdueRequestsDigest,
            "طلبات متأخرة عن مهلة الرد",
            $"يوجد {count} طلب تجاوز مهلة الرد المحددة وما زال بانتظار المراجعة.",
            cancellationToken);

    public Task NotifyPaymentConfirmationNeededDigestAsync(int count, CancellationToken cancellationToken = default)
        => NotifyOfficersAsync(
            NotificationTypes.PaymentConfirmationNeededDigest,
            "طلبات بانتظار تأكيد السداد",
            $"يوجد {count} طلب بانتظار تأكيد السداد.",
            cancellationToken);

    private Task NotifyOfficersOfNewWorkItemAsync(
        Request request, string title, string message, CancellationToken cancellationToken)
        => NotifyOfficersAsync(NotificationTypes.NewRequestArrived, title, message, cancellationToken, request);

    private async Task NotifyOfficersAsync(
        string type, string title, string message, CancellationToken cancellationToken, Request? request = null)
    {
        var officerIds = await context.Users
            .Where(u => u.RoleAssignments.Any(ra => ra.Role == UserRole.P3OOfficer) && u.IsActive && !u.DeletedAccount)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await WriteManyAsync(officerIds, type, title, message, request, cancellationToken);
    }

    private async Task WriteAsync(
        Guid recipientId, string type, string title, string message,
        Request? request, CancellationToken cancellationToken)
    {
        var isEnabled = await IsEnabledForRecipientAsync(recipientId, type, cancellationToken);

        await notificationRepository.AddAsync(new Notification
        {
            UserId = recipientId,
            RequestId = request?.Id,
            RequestNumberSnapshot = request?.RequestNumber,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedDate = DateTime.UtcNow,
            SuppressedByPreference = !isEnabled
        }, cancellationToken);
    }

    private async Task WriteManyAsync(
        List<Guid> recipientIds, string type, string title, string message,
        Request? request, CancellationToken cancellationToken)
    {
        if (recipientIds.Count == 0)
        {
            return;
        }

        var roleRows = await context.Users
            .Where(u => recipientIds.Contains(u.Id))
            .SelectMany(u => u.RoleAssignments.Select(ra => new { UserId = u.Id, ra.Role }))
            .ToListAsync(cancellationToken);
        var rolesByUser = roleRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Role).ToList());

        var keysByUser = recipientIds.ToDictionary(
            id => id,
            id => (rolesByUser.TryGetValue(id, out var roles) ? roles : [])
                .Select(role => PreferenceKeyFor(type, role))
                .Where(key => key is not null)
                .Cast<string>()
                .Distinct()
                .ToList());

        var allKeys = keysByUser.Values.SelectMany(keys => keys).Distinct().ToList();
        var storedByUserKey = allKeys.Count == 0
            ? new Dictionary<(Guid UserId, string PreferenceKey), bool>()
            : (await context.NotificationPreferences
                .Where(p => recipientIds.Contains(p.UserId) && allKeys.Contains(p.PreferenceKey))
                .ToListAsync(cancellationToken))
                .ToDictionary(p => (p.UserId, p.PreferenceKey), p => p.IsEnabled);

        var notifications = recipientIds.Select(recipientId =>
        {
            var keys = keysByUser[recipientId];
            var isEnabled = keys.Count == 0 || keys.Any(key =>
                !storedByUserKey.TryGetValue((recipientId, key), out var enabled) || enabled);

            return new Notification
            {
                UserId = recipientId,
                RequestId = request?.Id,
                RequestNumberSnapshot = request?.RequestNumber,
                Type = type,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedDate = DateTime.UtcNow,
                SuppressedByPreference = !isEnabled
            };
        }).ToList();

        await notificationRepository.AddManyAsync(notifications, cancellationToken);
    }

    private async Task<bool> IsEnabledForRecipientAsync(
        Guid recipientId, string type, CancellationToken cancellationToken)
    {
        var roles = await context.Users
            .Where(u => u.Id == recipientId)
            .SelectMany(u => u.RoleAssignments.Select(ra => ra.Role))
            .ToListAsync(cancellationToken);

        var preferenceKeys = roles
            .Select(role => PreferenceKeyFor(type, role))
            .Where(key => key is not null)
            .Cast<string>()
            .Distinct()
            .ToList();

        if (preferenceKeys.Count == 0)
        {
            return true;
        }

        foreach (var preferenceKey in preferenceKeys)
        {
            if (await preferenceService.IsEnabledAsync(recipientId, preferenceKey, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static string? PreferenceKeyFor(string type, UserRole role) => (type, role) switch
    {
        (NotificationTypes.AwaitingPayment, UserRole.ContractorConsultant)
            => NotificationPreferenceKeys.Beneficiary.RequestAccepted,
        (NotificationTypes.Rejected, UserRole.ContractorConsultant)
            => NotificationPreferenceKeys.Beneficiary.RequestRejected,
        (NotificationTypes.PaymentConfirmed, UserRole.ContractorConsultant)
            => NotificationPreferenceKeys.Beneficiary.PaymentConfirmed,
        (NotificationTypes.Activated, UserRole.ContractorConsultant)
            => NotificationPreferenceKeys.Beneficiary.RequestCompleted,
        (NotificationTypes.ReturnedForCorrection, UserRole.ContractorConsultant)
            => NotificationPreferenceKeys.Beneficiary.RequestReturned,

        (NotificationTypes.NewRequestArrived, UserRole.P3OOfficer)
            => NotificationPreferenceKeys.O3P.NewRequestArrived,
        (NotificationTypes.RequestCancelled, UserRole.P3OOfficer)
            => NotificationPreferenceKeys.O3P.RequestCancelled,
        (NotificationTypes.AccountChanged, UserRole.P3OOfficer)
            => NotificationPreferenceKeys.O3P.AccountsChanged,
        (NotificationTypes.OwnAccountChanged, UserRole.P3OOfficer)
            => NotificationPreferenceKeys.O3P.OwnAccountChanged,
        (NotificationTypes.OverdueRequestsDigest, UserRole.P3OOfficer)
            => NotificationPreferenceKeys.O3P.OverdueRequestsCount,
        (NotificationTypes.PaymentConfirmationNeededDigest, UserRole.P3OOfficer)
            => NotificationPreferenceKeys.O3P.PaymentConfirmationNeededCount,

        (NotificationTypes.AccountChanged, UserRole.SystemAdministrator)
            => NotificationPreferenceKeys.Admin.AccountsChanged,
        (NotificationTypes.OwnAccountChanged, UserRole.SystemAdministrator)
            => NotificationPreferenceKeys.Admin.OwnAccountChanged,
        (NotificationTypes.NewAuditLogEntry, UserRole.SystemAdministrator)
            => NotificationPreferenceKeys.Admin.NewAuditLogEntry,

        _ => null
    };

    private static string ApplicantNameOf(Request request)
    {
        var name = $"{request.ApplicantFirstName} {request.ApplicantLastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "مقدم الطلب" : name;
    }
}
