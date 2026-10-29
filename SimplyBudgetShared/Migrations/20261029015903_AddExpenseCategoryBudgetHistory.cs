using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoryBudgetHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseCategoryBudget",
                columns: table => new
                {
                    ExpenseCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveMonth = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BudgetedAmount = table.Column<int>(type: "INTEGER", nullable: false),
                    BudgetedPercentage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategoryBudget", x => new { x.ExpenseCategoryId, x.EffectiveMonth });
                    table.ForeignKey(
                        name: "FK_ExpenseCategoryBudget_ExpenseCategory_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategory",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseCategoryBudget");
        }
    }
}