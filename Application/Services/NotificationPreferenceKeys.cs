using PMIS.Domain.Enums;

namespace PMIS.Application.Services;

public static class NotificationPreferenceKeys
{
    public static class Beneficiary
    {
        public const string RequestAccepted = "requestAccepted";
        public const string RequestRejected = "requestRejected";
        public const string PaymentConfirmed = "paymentConfirmed";
        public const string RequestCompleted = "requestCompleted";
        public const string RequestReturned = "requestReturned";
    }

    public static class O3P
    {
        public const string NewRequestArrived = "newRequestArrived";
        public const string AccountsChanged = "accountsChanged";
        public const string OwnAccountChanged = "ownAccountChanged";
        public const string OverdueRequestsCount = "overdueRequestsCount";
        public const string PaymentConfirmationNeededCount = "paymentConfirmationNeededCount";
        public const string RequestCancelled = "requestCancelled";
    }

    public static class Admin
    {
        public const string AccountsChanged = "accountsChanged";
        public const string OwnAccountChanged = "ownAccountChanged";
        public const string NewRequestsCount = "newRequestsCount";
        public const string NewAuditLogEntry = "newAuditLogEntry";
    }

    public static readonly IReadOnlyDictionary<UserRole, IReadOnlyList<string>> ByRole =
        new Dictionary<UserRole, IReadOnlyList<string>>
        {
            [UserRole.ContractorConsultant] =
            [
                Beneficiary.RequestAccepted,
                Beneficiary.RequestRejected,
                Beneficiary.PaymentConfirmed,
                Beneficiary.RequestCompleted,
                Beneficiary.RequestReturned
            ],
            [UserRole.P3OOfficer] =
            [
                O3P.NewRequestArrived,
                O3P.AccountsChanged,
                O3P.OwnAccountChanged,
                O3P.OverdueRequestsCount,
                O3P.PaymentConfirmationNeededCount,
                O3P.RequestCancelled
            ],
            [UserRole.SystemAdministrator] =
            [
                Admin.AccountsChanged,
                Admin.OwnAccountChanged,
                Admin.NewRequestsCount,
                Admin.NewAuditLogEntry
            ]
        };

    public static IReadOnlyList<string> ForRole(UserRole role)
        => ByRole.TryGetValue(role, out var keys) ? keys : [];
}
