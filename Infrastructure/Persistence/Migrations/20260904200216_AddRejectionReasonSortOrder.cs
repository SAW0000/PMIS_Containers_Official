using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class AddRejectionReasonSortOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "RejectionReasonOptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "RejectionReasonOptions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                column: "SortOrder",
                value: 100);

            migrationBuilder.CreateIndex(
                name: "IX_RejectionReasonOptions_SortOrder_CreatedDate",
                table: "RejectionReasonOptions",
                columns: new[] { "SortOrder", "CreatedDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RejectionReasonOptions_SortOrder_CreatedDate",
                table: "RejectionReasonOptions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "RejectionReasonOptions");
        }
    }
}
