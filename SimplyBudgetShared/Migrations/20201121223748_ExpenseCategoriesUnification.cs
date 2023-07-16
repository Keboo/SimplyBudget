using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplyBudgetShared.Migrations;

public partial class ExpenseCategoriesUnification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExpenseCategoryItem",
            columns: table => new
            {
                ID = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseCategoryItem", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "ExpenseCategoryItemDetail",
            columns: table => new
            {
                ID = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ExpenseCategoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                ExpenseCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseCategoryItemDetail", x => x.ID);
                table.ForeignKey(
                    name: "FK_ExpenseCategoryItemDetail_ExpenseCategory_ExpenseCategoryId",
                    column: x => x.ExpenseCategoryId,
                    principalTable: "ExpenseCategory",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ExpenseCategoryItemDetail_ExpenseCategoryItem_ExpenseCategoryItemId",
                    column: x => x.ExpenseCategoryItemId,
                    principalTable: "ExpenseCategoryItem",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseCategoryItemDetail_ExpenseCategoryId",
            table: "ExpenseCategoryItemDetail",
            column: "ExpenseCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseCategoryItemDetail_ExpenseCategoryItemId",
            table: "ExpenseCategoryItemDetail",
            column: "ExpenseCategoryItemId");

        migrationBuilder.Sql(@"
INSERT INTO ExpenseCategoryItem (ID, Date, Description)
SELECT ID, Date, Description FROM Income;");
       migrationBuilder.Sql(@"
INSERT INTO ExpenseCategoryItemDetail (ExpenseCategoryItemId, ExpenseCategoryId, Amount)
SELECT IncomeID, ExpenseCategoryID, Amount FROM IncomeItem;");

        migrationBuilder.Sql(@"
INSERT INTO ExpenseCategoryItem (ID, Date, Description)
SELECT ID + (SELECT MAX(ID) FROM Income), Date, Description FROM ""Transaction"";");
        migrationBuilder.Sql(@"
INSERT INTO ExpenseCategoryItemDetail (ExpenseCategoryItemId, ExpenseCategoryId, Amount)
SELECT TransactionID + (SELECT MAX(ID) FROM Income), ExpenseCategoryID, -Amount FROM TransactionItem;");

        migrationBuilder.Sql(@"
INSERT INTO ExpenseCategoryItem (ID, Date, Description)
SELECT ID + (SELECT MAX(ID) FROM Income) + (SELECT MAX(ID) FROM ""Transaction""), Date, Description FROM Transfer;");
        migrationBuilder.Sql(@"
INSERT INTO ExpenseCategoryItemDetail (ExpenseCategoryItemId, ExpenseCategoryId, Amount)
SELECT ID + (SELECT MAX(ID) FROM Income) + (SELECT MAX(ID) FROM ""Transaction""), FromExpenseCategoryID, -Amount FROM Transfer;");
        migrationBuilder.Sql(@"
INSERT INTO ExpenseCategoryItemDetail (ExpenseCategoryItemId, ExpenseCategoryId, Amount)
SELECT ID + (SELECT MAX(ID) FROM Income) + (SELECT MAX(ID) FROM ""Transaction""), ToExpenseCategoryID, Amount FROM Transfer;");

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ExpenseCategoryItemDetail");

        migrationBuilder.DropTable(
            name: "ExpenseCategoryItem");
    }
}
