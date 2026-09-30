using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    public partial class DecoupleClusteredIndexFromGuidPk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropForeignKeys(migrationBuilder);

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoleAssignments",
                table: "UserRoleAssignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TargetSystems",
                table: "TargetSystems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SystemSettings",
                table: "SystemSettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Requests",
                table: "Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestNotes",
                table: "RequestNotes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestHistories",
                table: "RequestHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RejectionReasonOptions",
                table: "RejectionReasonOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationPreferences",
                table: "NotificationPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Licenses",
                table: "Licenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companies",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BankAccounts",
                table: "BankAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "UserRoleAssignments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "TargetSystems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "SystemSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "Requests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "RequestNotes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "RequestHistories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "RejectionReasonOptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "Projects",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "NotificationPreferences",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "Licenses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "Companies",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "BankAccounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "RowId",
                table: "Attachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoleAssignments",
                table: "UserRoleAssignments",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TargetSystems",
                table: "TargetSystems",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SystemSettings",
                table: "SystemSettings",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Requests",
                table: "Requests",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestNotes",
                table: "RequestNotes",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestHistories",
                table: "RequestHistories",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RejectionReasonOptions",
                table: "RejectionReasonOptions",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationPreferences",
                table: "NotificationPreferences",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Licenses",
                table: "Licenses",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companies",
                table: "Companies",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BankAccounts",
                table: "BankAccounts",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RowId",
                table: "Users",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_RowId",
                table: "UserRoleAssignments",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_TargetSystems_RowId",
                table: "TargetSystems",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_RowId",
                table: "SystemSettings",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RowId",
                table: "Requests",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestNotes_RowId",
                table: "RequestNotes",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistories_RowId",
                table: "RequestHistories",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_RejectionReasonOptions_RowId",
                table: "RejectionReasonOptions",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RowId",
                table: "Projects",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_RowId",
                table: "NotificationPreferences",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_RowId",
                table: "Licenses",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_RowId",
                table: "Companies",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_RowId",
                table: "BankAccounts",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_RowId",
                table: "Attachments",
                column: "RowId",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            AddForeignKeys(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropForeignKeys(migrationBuilder);

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RowId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoleAssignments",
                table: "UserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleAssignments_RowId",
                table: "UserRoleAssignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TargetSystems",
                table: "TargetSystems");

            migrationBuilder.DropIndex(
                name: "IX_TargetSystems_RowId",
                table: "TargetSystems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SystemSettings",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_RowId",
                table: "SystemSettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Requests",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_RowId",
                table: "Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestNotes",
                table: "RequestNotes");

            migrationBuilder.DropIndex(
                name: "IX_RequestNotes_RowId",
                table: "RequestNotes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestHistories",
                table: "RequestHistories");

            migrationBuilder.DropIndex(
                name: "IX_RequestHistories_RowId",
                table: "RequestHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RejectionReasonOptions",
                table: "RejectionReasonOptions");

            migrationBuilder.DropIndex(
                name: "IX_RejectionReasonOptions_RowId",
                table: "RejectionReasonOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_RowId",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationPreferences",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_RowId",
                table: "NotificationPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Licenses",
                table: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_Licenses_RowId",
                table: "Licenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companies",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_RowId",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BankAccounts",
                table: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_RowId",
                table: "BankAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_RowId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "TargetSystems");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "RequestNotes");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "RequestHistories");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "RejectionReasonOptions");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Attachments");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoleAssignments",
                table: "UserRoleAssignments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TargetSystems",
                table: "TargetSystems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SystemSettings",
                table: "SystemSettings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Requests",
                table: "Requests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestNotes",
                table: "RequestNotes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestHistories",
                table: "RequestHistories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RejectionReasonOptions",
                table: "RejectionReasonOptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationPreferences",
                table: "NotificationPreferences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Licenses",
                table: "Licenses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companies",
                table: "Companies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BankAccounts",
                table: "BankAccounts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments",
                column: "Id");

            AddForeignKeys(migrationBuilder);
        }

        private static void DropForeignKeys(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Requests_RequestId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Licenses_Requests_RequestId",
                table: "Licenses");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationPreferences_Users_UserId",
                table: "NotificationPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestHistories_Requests_RequestId",
                table: "RequestHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotes_RejectionReasonOptions_ReasonId",
                table: "RequestNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotes_Requests_RequestId",
                table: "RequestNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotes_Users_UserId",
                table: "RequestNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Companies_CompanyId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Projects_ProjectId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_TargetSystems_SystemId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_ReviewedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_UserId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAssignments_Users_UserId",
                table: "UserRoleAssignments");
        }

        private static void AddForeignKeys(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Requests_RequestId",
                table: "Attachments",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Licenses_Requests_RequestId",
                table: "Licenses",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationPreferences_Users_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestHistories_Requests_RequestId",
                table: "RequestHistories",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotes_RejectionReasonOptions_ReasonId",
                table: "RequestNotes",
                column: "ReasonId",
                principalTable: "RejectionReasonOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotes_Requests_RequestId",
                table: "RequestNotes",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotes_Users_UserId",
                table: "RequestNotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Companies_CompanyId",
                table: "Requests",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Projects_ProjectId",
                table: "Requests",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_TargetSystems_SystemId",
                table: "Requests",
                column: "SystemId",
                principalTable: "TargetSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_ReviewedById",
                table: "Requests",
                column: "ReviewedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_UserId",
                table: "Requests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAssignments_Users_UserId",
                table: "UserRoleAssignments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
