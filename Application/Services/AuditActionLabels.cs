namespace PMIS.Application.Services;

public static class AuditActionLabels
{
    public static string For(string action) => action switch
    {
        "RequestCreated" => "إنشاء الطلب",
        "RequestSubmitted" => "إرسال الطلب",
        "RequestResubmitted" => "إعادة إرسال الطلب",
        "RequestReturned" => "إعادة طلب للمستفيد",
        "RequestRejected" => "رفض الطلب",
        "RequestApproved" => "اعتماد الطلب",
        "PaymentConfirmed" => "تأكيد سداد",
        "LicenseActivated" => "تفعيل الرخصة",
        "RequestCancelled" => "إلغاء الطلب",
        "DraftSaved" => "حفظ طلب كمسودة",
        "AttachmentUploaded" => "رفع مرفق",
        "AttachmentRejected" => "رفض مرفق (فحص أمني)",
        "UserLoggedIn" => "تسجيل الدخول",
        "UserLoginFailed" => "محاولة تسجيل دخول فاشلة",
        "UserCreated" => "إنشاء مستخدم",
        "UserUpdated" => "تعديل بيانات مستخدم",
        "UserSuspended" => "تعليق مستخدم",
        "UserDeleted" => "حذف حساب مستخدم",
        "SettingsChanged" => "تعديل إعدادات النظام",
        "PasswordChanged" => "تغيير كلمة المرور",
        "AuditLogCleared" => "حذف سجل عمليات النظام",
        "NotificationLogCleared" => "حذف سجل إشعارات المستخدمين",
        _ => action
    };
}
