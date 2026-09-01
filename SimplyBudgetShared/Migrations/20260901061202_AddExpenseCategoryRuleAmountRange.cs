using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoryRuleAmountRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximumAmount",
                table: "ExpenseCategoryRules",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumAmount",
                table: "ExpenseCategoryRules",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumAmount",
                table: "ExpenseCategoryRules");

            migrationBuilder.DropColumn(
                name: "MinimumAmount",
                table: "ExpenseCategoryRules");
        }
    }
}
