using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class MSSqlCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategoryItem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategoryItem", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MetaData",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaData", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategory",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AccountID = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BudgetedPercentage = table.Column<int>(type: "int", nullable: false),
                    BudgetedAmount = table.Column<int>(type: "int", nullable: false),
                    CurrentBalance = table.Column<int>(type: "int", nullable: false),
                    Cap = table.Column<int>(type: "int", nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExpenseCategory_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategoryItemDetail",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseCategoryItemId = table.Column<int>(type: "int", nullable: false),
                    ExpenseCategoryId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    IgnoreBudget = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategoryItemDetail", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExpenseCategoryItemDetail_ExpenseCategoryItem_ExpenseCategoryItemId",
                        column: x => x.ExpenseCategoryItemId,
                        principalTable: "ExpenseCategoryItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpenseCategoryItemDetail_ExpenseCategory_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategory",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategoryRules",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuleRegex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpenseCategoryID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategoryRules", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExpenseCategoryRules_ExpenseCategory_ExpenseCategoryID",
                        column: x => x.ExpenseCategoryID,
                        principalTable: "ExpenseCategory",
                        principalColumn: "ID");
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
                name: "IX_ExpenseCategoryItemDetail_ExpenseCategoryId",
                table: "ExpenseCategoryItemDetail",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategoryItemDetail_ExpenseCategoryItemId",
                table: "ExpenseCategoryItemDetail",
                column: "ExpenseCategoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategoryRules_ExpenseCategoryID",
                table: "ExpenseCategoryRules",
                column: "ExpenseCategoryID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseCategoryItemDetail");

            migrationBuilder.DropTable(
                name: "ExpenseCategoryRules");

            migrationBuilder.DropTable(
                name: "MetaData");

            migrationBuilder.DropTable(
                name: "ExpenseCategoryItem");

            migrationBuilder.DropTable(
                name: "ExpenseCategory");

            migrationBuilder.DropTable(
                name: "Account");
        }
    }
}
