using PMIS.Application.DTOs;

namespace PMIS.Domain.ValueObjects;

public static class OtpResultMessages
{
    public const string InfrastructureProblem = "توجد مشكلة في عملية التحقق";
    public const string WrongCode = "خطأ في عملية التحقق";

    public static string? ForSendFailure(OtpSendResult result) => result switch
    {
        OtpSendResult.Sent => null,
        OtpSendResult.CooldownActive => "يرجى الانتظار قبل إعادة إرسال الرمز",
        OtpSendResult.ResendLimitReached => "تم تجاوز الحد الأقصى لإعادة إرسال رمز التحقق",
        _ => InfrastructureProblem
    };

    public static string? ForVerifyFailure(OtpVerifyResult result) => result switch
    {
        OtpVerifyResult.Verified => null,
        OtpVerifyResult.CodeMismatch => WrongCode,
        _ => InfrastructureProblem
    };
}
