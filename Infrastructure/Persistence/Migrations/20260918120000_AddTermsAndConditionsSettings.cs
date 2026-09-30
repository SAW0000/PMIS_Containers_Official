using System;
using Microsoft.EntityFrameworkCore.Migrations;
using PMIS.Domain.Entities;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddTermsAndConditionsSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TermsAndConditionsText",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: SystemSettings.DefaultTermsAndConditionsText);

            migrationBuilder.AddColumn<string>(
                name: "TermsAgreementLabel",
                table: "SystemSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: SystemSettings.DefaultTermsAgreementLabel);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: ["TermsAndConditionsText", "TermsAgreementLabel"],
                values: [SystemSettings.DefaultTermsAndConditionsText, SystemSettings.DefaultTermsAgreementLabel]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermsAndConditionsText",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TermsAgreementLabel",
                table: "SystemSettings");
        }
    }
}
