using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplyBudgetShared.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    ValidatedDate = table.Column<DateTime>(nullable: false),
                    IsDefault = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Income",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(nullable: false),
                    TotalAmount = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Income", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "IncomeItem",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IncomeID = table.Column<int>(nullable: false),
                    ExpenseCategoryID = table.Column<int>(nullable: false),
                    Amount = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeItem", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MetaData",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaData", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(nullable: false),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TransactionItem",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransactionID = table.Column<int>(nullable: false),
                    ExpenseCategoryID = table.Column<int>(nullable: false),
                    Amount = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionItem", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Transfer",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromExpenseCategoryID = table.Column<int>(nullable: false),
                    ToExpenseCategoryID = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Amount = table.Column<int>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfer", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategory",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryName = table.Column<string>(nullable: true),
                    AccountID = table.Column<int>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    BudgetedPercentage = table.Column<int>(nullable: false),
                    BudgetedAmount = table.Column<int>(nullable: false),
                    CurrentBalance = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExpenseCategory_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_IsDefault",
                table: "Account",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_AccountID",
                table: "ExpenseCategory",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_CategoryName",
                table: "ExpenseCategory",
                column: "CategoryName");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseCategory");

            migrationBuilder.DropTable(
                name: "Income");

            migrationBuilder.DropTable(
                name: "IncomeItem");

            migrationBuilder.DropTable(
                name: "MetaData");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "TransactionItem");

            migrationBuilder.DropTable(
                name: "Transfer");

            migrationBuilder.DropTable(
                name: "Account");
        }
    }
}
