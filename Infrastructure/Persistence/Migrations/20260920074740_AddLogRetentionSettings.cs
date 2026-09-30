using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddLogRetentionSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuditLogMaxCount",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuditLogRetentionDays",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationMaxCount",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationRetentionDays",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "AuditLogMaxCount", "AuditLogRetentionDays", "NotificationMaxCount", "NotificationRetentionDays" },
                values: new object[] { 0, 0, 0, 0 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditLogMaxCount",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AuditLogRetentionDays",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "NotificationMaxCount",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "NotificationRetentionDays",
                table: "SystemSettings");
        }
    }
}
