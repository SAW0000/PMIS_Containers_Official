using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class RejectionReasonOptionConfiguration : IEntityTypeConfiguration<RejectionReasonOption>
{
    public static readonly Guid DocumentsReasonId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    public static readonly Guid ContractReasonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    public static readonly Guid IneligibleReasonId = Guid.Parse("00000000-0000-0000-0000-000000000103");
    public static readonly Guid OtherReasonId = Guid.Parse("00000000-0000-0000-0000-000000000104");

    public const int TrailingSortOrder = 100;

    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<RejectionReasonOption> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("NEWID()");

        builder.Property(o => o.Label).IsRequired().HasMaxLength(80);
        builder.Property(o => o.DefaultNote).IsRequired().HasMaxLength(2000);
        builder.Property(o => o.Hint).HasMaxLength(200);
        builder.Property(o => o.SortOrder).IsRequired().HasDefaultValue(0);

        builder.HasIndex(o => new { o.SortOrder, o.CreatedDate });

        builder.HasIndex(o => o.Label).IsUnique();

        builder.HasData(
            new RejectionReasonOption
            {
                Id = DocumentsReasonId,
                Label = "نقص أو خطأ في المستندات",
                Hint = "سجل تجاري، ضريبة القيمة المضافة، عنوان وطني...",
                DefaultNote = "تبيّن وجود نقص أو خطأ في المستندات المرفقة (سجل تجاري/ضريبة القيمة المضافة/عنوان وطني). يرجى مراجعة المرفقات وإعادة تقديم الطلب.",
                IsActive = true,
                CreatedDate = SeedDate
            },
            new RejectionReasonOption
            {
                Id = ContractReasonId,
                Label = "خلل في بيانات العقد",
                Hint = "رقم العقد المرجعي غير مطابق أو منتهٍ",
                DefaultNote = "رقم العقد المرجعي المدخل غير مطابق لسجلات الأمانة أو أن العقد منتهي الصلاحية. يرجى التحقق من بيانات العقد وإعادة المحاولة.",
                IsActive = true,
                CreatedDate = SeedDate
            },
            new RejectionReasonOption
            {
                Id = IneligibleReasonId,
                Label = "مقدّم الطلب غير مؤهل",
                Hint = "لا يستوفي شروط الاستفادة من الخدمة",
                DefaultNote = "الشركة/مقدّم الطلب لا يستوفي شروط الاستفادة من هذه الخدمة حاليًا.",
                IsActive = true,
                CreatedDate = SeedDate
            },
            new RejectionReasonOption
            {
                Id = OtherReasonId,
                Label = "سبب آخر",
                Hint = "يُذكر تفصيليًا في الملاحظة أدناه",
                DefaultNote = string.Empty,
                SortOrder = TrailingSortOrder,
                IsActive = true,
                CreatedDate = SeedDate
            }
        );
    }
}
