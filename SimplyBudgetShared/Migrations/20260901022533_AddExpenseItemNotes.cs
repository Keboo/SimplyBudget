using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseItemNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ExpenseCategoryItem",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ExpenseCategoryItem");
        }
    }
}
