namespace PMIS.Domain.Enums;

public enum RequestStatus
{
    Draft,
    Submitted,
    ReturnedForCorrection,
    Approved,
    Rejected,
    AwaitingPaymentConfirmation,
    PaymentConfirmed,
    Activated,
    Cancelled
}
