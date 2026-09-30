using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddNafathIdentityFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArabicFatherName",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArabicFirstName",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArabicGrandFatherName",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArabicNationality",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DobGregorian",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArabicFatherName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ArabicFirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ArabicGrandFatherName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ArabicNationality",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DobGregorian",
                table: "Users");
        }
    }
}
