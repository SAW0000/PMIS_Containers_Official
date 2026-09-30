namespace PMIS.Application.Services;

public static class NotificationTypes
{
    public const string RequestSubmitted = "RequestSubmitted";
    public const string ReturnedForCorrection = "ReturnedForCorrection";
    public const string Rejected = "Rejected";

    public const string AwaitingPayment = "AwaitingPayment";

    public const string PaymentConfirmed = "PaymentConfirmed";
    public const string Activated = "Activated";

    public const string RequestCancelled = "RequestCancelled";

    public const string NewRequestArrived = "NewRequestArrived";

    public const string AccountChanged = "AccountChanged";
    public const string OwnAccountChanged = "OwnAccountChanged";

    public const string NewAuditLogEntry = "NewAuditLogEntry";

    public const string OverdueRequestsDigest = "OverdueRequestsDigest";

    public const string PaymentConfirmationNeededDigest = "PaymentConfirmationNeededDigest";
}
