using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddOtpSettingsToSystemSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OtpExpiryMinutes",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OtpMaxResendCount",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OtpResendCooldownSeconds",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "OtpExpiryMinutes", "OtpMaxResendCount", "OtpResendCooldownSeconds" },
                values: new object[] { 5, 5, 45 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpExpiryMinutes",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "OtpMaxResendCount",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "OtpResendCooldownSeconds",
                table: "SystemSettings");
        }
    }
}
