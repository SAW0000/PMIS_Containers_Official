namespace PMIS.Application.DTOs;

public enum OtpSendResult
{
    Sent,

    DeliveryFailed,

    CooldownActive,

    ResendLimitReached
}
