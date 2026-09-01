using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoryRuleAmountRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximumAmount",
                schema: "SimplyBudget",
                table: "ExpenseCategoryRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumAmount",
                schema: "SimplyBudget",
                table: "ExpenseCategoryRules",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumAmount",
                schema: "SimplyBudget",
                table: "ExpenseCategoryRules");

            migrationBuilder.DropColumn(
                name: "MinimumAmount",
                schema: "SimplyBudget",
                table: "ExpenseCategoryRules");
        }
    }
}
