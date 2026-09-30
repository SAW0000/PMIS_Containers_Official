using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddOtpMessageTemplatesToSystemSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OtpEmailMessageTemplate",
                table: "SystemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OtpSmsMessageTemplate",
                table: "SystemSettings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "OtpEmailMessageTemplate", "OtpSmsMessageTemplate" },
                values: new object[] { "يرجى إدخال رمز التحقق: @MAIL", "يرجى إدخال رمز التحقق: @SMS" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpEmailMessageTemplate",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "OtpSmsMessageTemplate",
                table: "SystemSettings");
        }
    }
}
