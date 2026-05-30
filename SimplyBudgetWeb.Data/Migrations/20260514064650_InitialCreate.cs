using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SimplyBudget");

            migrationBuilder.CreateTable(
                name: "Account",
                schema: "SimplyBudget",
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
                schema: "SimplyBudget",
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
                schema: "SimplyBudget",
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
                schema: "SimplyBudget",
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
                        principalSchema: "SimplyBudget",
                        principalTable: "Account",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategoryItemDetail",
                schema: "SimplyBudget",
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
                        principalSchema: "SimplyBudget",
                        principalTable: "ExpenseCategoryItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpenseCategoryItemDetail_ExpenseCategory_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalSchema: "SimplyBudget",
                        principalTable: "ExpenseCategory",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategoryRules",
                schema: "SimplyBudget",
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
                        principalSchema: "SimplyBudget",
                        principalTable: "ExpenseCategory",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_IsDefault",
                schema: "SimplyBudget",
                table: "Account",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_AccountID",
                schema: "SimplyBudget",
                table: "ExpenseCategory",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_CategoryName",
                schema: "SimplyBudget",
                table: "ExpenseCategory",
                column: "CategoryName");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategoryItemDetail_ExpenseCategoryId",
                schema: "SimplyBudget",
                table: "ExpenseCategoryItemDetail",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategoryItemDetail_ExpenseCategoryItemId",
                schema: "SimplyBudget",
                table: "ExpenseCategoryItemDetail",
                column: "ExpenseCategoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategoryRules_ExpenseCategoryID",
                schema: "SimplyBudget",
                table: "ExpenseCategoryRules",
                column: "ExpenseCategoryID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseCategoryItemDetail",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "ExpenseCategoryRules",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "MetaData",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "ExpenseCategoryItem",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "ExpenseCategory",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "Account",
                schema: "SimplyBudget");
        }
    }
}
