using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplyBudgetShared.Migrations;

public partial class AddIgnoreBudget : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TransactionItem");

        migrationBuilder.DropTable(
            name: "Transfer");

        migrationBuilder.DropTable(
            name: "Transaction");

        migrationBuilder.AddColumn<bool>(
            name: "IgnoreBudget",
            table: "ExpenseCategoryItemDetail",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IgnoreBudget",
            table: "ExpenseCategoryItemDetail");

        migrationBuilder.CreateTable(
            name: "Transaction",
            columns: table => new
            {
                ID = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transaction", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "Transfer",
            columns: table => new
            {
                ID = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Amount = table.Column<int>(type: "INTEGER", nullable: false),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                FromExpenseCategoryID = table.Column<int>(type: "INTEGER", nullable: false),
                ToExpenseCategoryID = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transfer", x => x.ID);
                table.ForeignKey(
                    name: "FK_Transfer_ExpenseCategory_FromExpenseCategoryID",
                    column: x => x.FromExpenseCategoryID,
                    principalTable: "ExpenseCategory",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Transfer_ExpenseCategory_ToExpenseCategoryID",
                    column: x => x.ToExpenseCategoryID,
                    principalTable: "ExpenseCategory",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TransactionItem",
            columns: table => new
            {
                ID = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Amount = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                ExpenseCategoryID = table.Column<int>(type: "INTEGER", nullable: false),
                TransactionID = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransactionItem", x => x.ID);
                table.ForeignKey(
                    name: "FK_TransactionItem_ExpenseCategory_ExpenseCategoryID",
                    column: x => x.ExpenseCategoryID,
                    principalTable: "ExpenseCategory",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TransactionItem_Transaction_TransactionID",
                    column: x => x.TransactionID,
                    principalTable: "Transaction",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Transaction_Date",
            table: "Transaction",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionItem_ExpenseCategoryID",
            table: "TransactionItem",
            column: "ExpenseCategoryID");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionItem_TransactionID",
            table: "TransactionItem",
            column: "TransactionID");

        migrationBuilder.CreateIndex(
            name: "IX_Transfer_Date",
            table: "Transfer",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_Transfer_FromExpenseCategoryID",
            table: "Transfer",
            column: "FromExpenseCategoryID");

        migrationBuilder.CreateIndex(
            name: "IX_Transfer_ToExpenseCategoryID",
            table: "Transfer",
            column: "ToExpenseCategoryID");
    }
}
