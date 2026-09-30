using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddRejectionReasonLookup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReasonCategory",
                table: "RequestNotes");

            migrationBuilder.AddColumn<Guid>(
                name: "ReasonId",
                table: "RequestNotes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "RejectionReasonOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DefaultNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectionReasonOptions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RejectionReasonOptions",
                columns: new[] { "Id", "CreatedDate", "DefaultNote", "Hint", "IsActive", "Label" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "تبيّن وجود نقص أو خطأ في المستندات المرفقة (سجل تجاري/ضريبة القيمة المضافة/عنوان وطني). يرجى مراجعة المرفقات وإعادة تقديم الطلب.", "سجل تجاري، ضريبة القيمة المضافة، عنوان وطني...", true, "نقص أو خطأ في المستندات" },
                    { new Guid("00000000-0000-0000-0000-000000000102"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "رقم العقد المرجعي المدخل غير مطابق لسجلات الأمانة أو أن العقد منتهي الصلاحية. يرجى التحقق من بيانات العقد وإعادة المحاولة.", "رقم العقد المرجعي غير مطابق أو منتهٍ", true, "خلل في بيانات العقد" },
                    { new Guid("00000000-0000-0000-0000-000000000103"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "الشركة/مقدّم الطلب لا يستوفي شروط الاستفادة من هذه الخدمة حاليًا.", "لا يستوفي شروط الاستفادة من الخدمة", true, "مقدّم الطلب غير مؤهل" },
                    { new Guid("00000000-0000-0000-0000-000000000104"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "", "يُذكر تفصيليًا في الملاحظة أدناه", true, "سبب آخر" }
                });

            migrationBuilder.Sql(
                "UPDATE RequestNotes SET ReasonId = '00000000-0000-0000-0000-000000000104' " +
                "WHERE ReasonId = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateIndex(
                name: "IX_RequestNotes_ReasonId",
                table: "RequestNotes",
                column: "ReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_RejectionReasonOptions_Label",
                table: "RejectionReasonOptions",
                column: "Label",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotes_RejectionReasonOptions_ReasonId",
                table: "RequestNotes",
                column: "ReasonId",
                principalTable: "RejectionReasonOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotes_RejectionReasonOptions_ReasonId",
                table: "RequestNotes");

            migrationBuilder.DropTable(
                name: "RejectionReasonOptions");

            migrationBuilder.DropIndex(
                name: "IX_RequestNotes_ReasonId",
                table: "RequestNotes");

            migrationBuilder.DropColumn(
                name: "ReasonId",
                table: "RequestNotes");

            migrationBuilder.AddColumn<string>(
                name: "ReasonCategory",
                table: "RequestNotes",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");
        }
    }
}
