using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddRequestHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestHistories_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistories_RequestId_EventDate",
                table: "RequestHistories",
                columns: new[] { "RequestId", "EventDate" });

            migrationBuilder.Sql(@"
                INSERT INTO RequestHistories (Id, RequestId, EventLabel, EventDate, IsActive, CreatedDate)
                SELECT NEWID(), r.Id,
                    CASE WHEN r.Status = 'Draft' THEN N'إنشاء الطلب (مسودة)' ELSE N'إنشاء الطلب' END,
                    r.RequestDate, 1, SYSUTCDATETIME()
                FROM Requests r;

                INSERT INTO RequestHistories (Id, RequestId, EventLabel, EventDate, IsActive, CreatedDate)
                SELECT NEWID(), r.Id, N'إرسال الطلب', r.RequestDate, 1, SYSUTCDATETIME()
                FROM Requests r
                WHERE r.Status <> 'Draft';

                INSERT INTO RequestHistories (Id, RequestId, EventLabel, EventDate, IsActive, CreatedDate)
                SELECT NEWID(), r.Id,
                    CASE r.Status
                        WHEN 'ReturnedForCorrection' THEN N'إعادة طلب للمستفيد'
                        WHEN 'Rejected' THEN N'رفض الطلب'
                        ELSE N'مراجعة مكتب P3O'
                    END,
                    r.ReviewDate, 1, SYSUTCDATETIME()
                FROM Requests r
                WHERE r.ReviewDate IS NOT NULL;

                INSERT INTO RequestHistories (Id, RequestId, EventLabel, EventDate, IsActive, CreatedDate)
                SELECT NEWID(), r.Id, N'قبول الطلب — بانتظار السداد', r.ReviewDate, 1, SYSUTCDATETIME()
                FROM Requests r
                WHERE r.ReviewDate IS NOT NULL
                    AND r.Status IN ('AwaitingPaymentConfirmation', 'PaymentConfirmed', 'Activated');

                INSERT INTO RequestHistories (Id, RequestId, EventLabel, EventDate, IsActive, CreatedDate)
                SELECT NEWID(), r.Id, N'اعتماد الطلب', r.PaymentConfirmedDate, 1, SYSUTCDATETIME()
                FROM Requests r
                WHERE r.PaymentConfirmedDate IS NOT NULL;

                INSERT INTO RequestHistories (Id, RequestId, EventLabel, EventDate, IsActive, CreatedDate)
                SELECT NEWID(), r.Id, N'تفعيل الرخصة', l.ActivationDate, 1, SYSUTCDATETIME()
                FROM Requests r
                JOIN Licenses l ON l.RequestId = r.Id
                WHERE l.ActivationDate IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestHistories");
        }
    }
}
