using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class ConvertUserGenderToArabicLabels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Users] SET [Gender] = N'ذكر' " +
                "WHERE [Gender] IS NOT NULL AND UPPER([Gender]) IN (N'M', N'MALE');");

            migrationBuilder.Sql(
                "UPDATE [Users] SET [Gender] = N'أنثى' " +
                "WHERE [Gender] IS NOT NULL AND (UPPER([Gender]) IN (N'F', N'FEMALE') OR [Gender] = N'انثى');");

            migrationBuilder.Sql(
                "UPDATE [Users] SET [Gender] = NULL " +
                "WHERE [Gender] IS NOT NULL AND [Gender] NOT IN (N'ذكر', N'أنثى');");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Gender",
                table: "Users",
                sql: "[Gender] IS NULL OR [Gender] IN (N'ذكر', N'أنثى')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Gender",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
