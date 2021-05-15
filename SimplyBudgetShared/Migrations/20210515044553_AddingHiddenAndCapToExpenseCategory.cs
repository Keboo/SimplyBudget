using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplyBudgetShared.Migrations
{
    public partial class AddingHiddenAndCapToExpenseCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncomeItem");

            migrationBuilder.DropTable(
                name: "Income");

            migrationBuilder.AddColumn<int>(
                name: "Cap",
                table: "ExpenseCategory",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "ExpenseCategory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cap",
                table: "ExpenseCategory");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "ExpenseCategory");

            migrationBuilder.CreateTable(
                name: "Income",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Income", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "IncomeItem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ExpenseCategoryID = table.Column<int>(type: "INTEGER", nullable: false),
                    IncomeID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_IncomeItem_ExpenseCategory_ExpenseCategoryID",
                        column: x => x.ExpenseCategoryID,
                        principalTable: "ExpenseCategory",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncomeItem_Income_IncomeID",
                        column: x => x.IncomeID,
                        principalTable: "Income",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncomeItem_ExpenseCategoryID",
                table: "IncomeItem",
                column: "ExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeItem_IncomeID",
                table: "IncomeItem",
                column: "IncomeID");
        }
    }
}
