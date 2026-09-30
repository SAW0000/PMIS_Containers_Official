using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddBankAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Iban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TransferAmount = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BankAccounts",
                columns: new[] { "Id", "AccountHolderName", "BankName", "CreatedDate", "Iban", "IsActive", "TransferAmount" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000201"), "أمانة منطقة المدينة المنورة", "مصرف الراجحي", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SA03 8000 0000 6080 1016 7519", true, "5000" });

            migrationBuilder.InsertData(
                table: "BankAccounts",
                columns: new[] { "Id", "AccountHolderName", "BankName", "CreatedDate", "Iban", "IsActive", "SortOrder", "TransferAmount" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000202"), "أمانة منطقة المدينة المنورة", "البنك الأهلي السعودي", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SA44 2000 0001 2345 6789 1234", true, 1, "7500" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_Iban",
                table: "BankAccounts",
                column: "Iban",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_SortOrder_CreatedDate",
                table: "BankAccounts",
                columns: new[] { "SortOrder", "CreatedDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccounts");
        }
    }
}
