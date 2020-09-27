using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplyBudgetShared.Migrations
{
    public partial class RelatedTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Transfers",
                table: "Transfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionItems",
                table: "TransactionItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MetaDatas",
                table: "MetaDatas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Incomes",
                table: "Incomes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IncomeItems",
                table: "IncomeItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpenseCategories",
                table: "ExpenseCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts");

            migrationBuilder.RenameTable(
                name: "Transfers",
                newName: "Transfer");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transaction");

            migrationBuilder.RenameTable(
                name: "TransactionItems",
                newName: "TransactionItem");

            migrationBuilder.RenameTable(
                name: "MetaDatas",
                newName: "MetaData");

            migrationBuilder.RenameTable(
                name: "Incomes",
                newName: "Income");

            migrationBuilder.RenameTable(
                name: "IncomeItems",
                newName: "IncomeItem");

            migrationBuilder.RenameTable(
                name: "ExpenseCategories",
                newName: "ExpenseCategory");

            migrationBuilder.RenameTable(
                name: "Accounts",
                newName: "Account");

            migrationBuilder.AlterColumn<int>(
                name: "AccountID",
                table: "ExpenseCategory",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transfer",
                table: "Transfer",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionItem",
                table: "TransactionItem",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MetaData",
                table: "MetaData",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Income",
                table: "Income",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IncomeItem",
                table: "IncomeItem",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpenseCategory",
                table: "ExpenseCategory",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Account",
                table: "Account",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_Date",
                table: "Transfer",
                column: "Date");

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
                name: "IX_ExpenseCategory_AccountID",
                table: "ExpenseCategory",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_CategoryName",
                table: "ExpenseCategory",
                column: "CategoryName");

            migrationBuilder.CreateIndex(
                name: "IX_Account_IsDefault",
                table: "Account",
                column: "IsDefault");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategory_Account_AccountID",
                table: "ExpenseCategory",
                column: "AccountID",
                principalTable: "Account",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategory_Account_AccountID",
                table: "ExpenseCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transfer",
                table: "Transfer");

            migrationBuilder.DropIndex(
                name: "IX_Transfer_Date",
                table: "Transfer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionItem",
                table: "TransactionItem");

            migrationBuilder.DropIndex(
                name: "IX_TransactionItem_ExpenseCategoryID",
                table: "TransactionItem");

            migrationBuilder.DropIndex(
                name: "IX_TransactionItem_TransactionID",
                table: "TransactionItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_Date",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MetaData",
                table: "MetaData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IncomeItem",
                table: "IncomeItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Income",
                table: "Income");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpenseCategory",
                table: "ExpenseCategory");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategory_AccountID",
                table: "ExpenseCategory");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategory_CategoryName",
                table: "ExpenseCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Account",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_IsDefault",
                table: "Account");

            migrationBuilder.RenameTable(
                name: "Transfer",
                newName: "Transfers");

            migrationBuilder.RenameTable(
                name: "TransactionItem",
                newName: "TransactionItems");

            migrationBuilder.RenameTable(
                name: "Transaction",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "MetaData",
                newName: "MetaDatas");

            migrationBuilder.RenameTable(
                name: "IncomeItem",
                newName: "IncomeItems");

            migrationBuilder.RenameTable(
                name: "Income",
                newName: "Incomes");

            migrationBuilder.RenameTable(
                name: "ExpenseCategory",
                newName: "ExpenseCategories");

            migrationBuilder.RenameTable(
                name: "Account",
                newName: "Accounts");

            migrationBuilder.AlterColumn<int>(
                name: "AccountID",
                table: "ExpenseCategories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transfers",
                table: "Transfers",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionItems",
                table: "TransactionItems",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MetaDatas",
                table: "MetaDatas",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IncomeItems",
                table: "IncomeItems",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Incomes",
                table: "Incomes",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpenseCategories",
                table: "ExpenseCategories",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts",
                column: "ID");
        }
    }
}
