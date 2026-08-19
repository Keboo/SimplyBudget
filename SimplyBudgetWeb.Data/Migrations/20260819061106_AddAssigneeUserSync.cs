using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssigneeUserSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PendingExpenseAssignee_Name",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginUtc",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ObjectId",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PendingExpenseAssignee_ObjectId",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                column: "ObjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PendingExpenseAssignee_ObjectId",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee");

            migrationBuilder.DropColumn(
                name: "LastLoginUtc",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee");

            migrationBuilder.DropColumn(
                name: "ObjectId",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PendingExpenseAssignee_Name",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                column: "Name",
                unique: true);
        }
    }
}
