using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class ReplaceSenderEmailWithAllowedAttachmentExtensions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationSenderEmail",
                table: "SystemSettings");

            migrationBuilder.AddColumn<string>(
                name: "AllowedAttachmentExtensions",
                table: "SystemSettings",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "PDF,JPG,PNG");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "AllowedAttachmentExtensions",
                value: "PDF,JPG,PNG");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedAttachmentExtensions",
                table: "SystemSettings");

            migrationBuilder.AddColumn<string>(
                name: "NotificationSenderEmail",
                table: "SystemSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "no-reply@madinah.gov.sa");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "NotificationSenderEmail",
                value: "no-reply@madinah.gov.sa");
        }
    }
}
