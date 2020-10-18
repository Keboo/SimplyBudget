using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplyBudgetShared.Migrations
{
    public partial class EntityRelationships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Transfer_FromExpenseCategoryID",
                table: "Transfer",
                column: "FromExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_ToExpenseCategoryID",
                table: "Transfer",
                column: "ToExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeItem_ExpenseCategoryID",
                table: "IncomeItem",
                column: "ExpenseCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeItem_IncomeID",
                table: "IncomeItem",
                column: "IncomeID");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeItem_ExpenseCategory_ExpenseCategoryID",
                table: "IncomeItem",
                column: "ExpenseCategoryID",
                principalTable: "ExpenseCategory",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeItem_Income_IncomeID",
                table: "IncomeItem",
                column: "IncomeID",
                principalTable: "Income",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionItem_ExpenseCategory_ExpenseCategoryID",
                table: "TransactionItem",
                column: "ExpenseCategoryID",
                principalTable: "ExpenseCategory",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionItem_Transaction_TransactionID",
                table: "TransactionItem",
                column: "TransactionID",
                principalTable: "Transaction",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfer_ExpenseCategory_FromExpenseCategoryID",
                table: "Transfer",
                column: "FromExpenseCategoryID",
                principalTable: "ExpenseCategory",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfer_ExpenseCategory_ToExpenseCategoryID",
                table: "Transfer",
                column: "ToExpenseCategoryID",
                principalTable: "ExpenseCategory",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncomeItem_ExpenseCategory_ExpenseCategoryID",
                table: "IncomeItem");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomeItem_Income_IncomeID",
                table: "IncomeItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionItem_ExpenseCategory_ExpenseCategoryID",
                table: "TransactionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionItem_Transaction_TransactionID",
                table: "TransactionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfer_ExpenseCategory_FromExpenseCategoryID",
                table: "Transfer");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfer_ExpenseCategory_ToExpenseCategoryID",
                table: "Transfer");

            migrationBuilder.DropIndex(
                name: "IX_Transfer_FromExpenseCategoryID",
                table: "Transfer");

            migrationBuilder.DropIndex(
                name: "IX_Transfer_ToExpenseCategoryID",
                table: "Transfer");

            migrationBuilder.DropIndex(
                name: "IX_IncomeItem_ExpenseCategoryID",
                table: "IncomeItem");

            migrationBuilder.DropIndex(
                name: "IX_IncomeItem_IncomeID",
                table: "IncomeItem");
        }
    }
}
