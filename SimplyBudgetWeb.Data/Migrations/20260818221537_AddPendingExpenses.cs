using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingExpenseAssignee",
                schema: "SimplyBudget",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingExpenseAssignee", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PendingExpense",
                schema: "SimplyBudget",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    IsDebit = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssigneeId = table.Column<int>(type: "int", nullable: true),
                    SuggestedCategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingExpense", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PendingExpense_ExpenseCategory_SuggestedCategoryId",
                        column: x => x.SuggestedCategoryId,
                        principalSchema: "SimplyBudget",
                        principalTable: "ExpenseCategory",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PendingExpense_PendingExpenseAssignee_AssigneeId",
                        column: x => x.AssigneeId,
                        principalSchema: "SimplyBudget",
                        principalTable: "PendingExpenseAssignee",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingExpense_AssigneeId",
                schema: "SimplyBudget",
                table: "PendingExpense",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingExpense_Date",
                schema: "SimplyBudget",
                table: "PendingExpense",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PendingExpense_SuggestedCategoryId",
                schema: "SimplyBudget",
                table: "PendingExpense",
                column: "SuggestedCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingExpenseAssignee_Name",
                schema: "SimplyBudget",
                table: "PendingExpenseAssignee",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingExpense",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "PendingExpenseAssignee",
                schema: "SimplyBudget");
        }
    }
}
