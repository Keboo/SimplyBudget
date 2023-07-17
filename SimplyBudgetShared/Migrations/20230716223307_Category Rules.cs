using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class CategoryRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseCategoryRules",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    RuleRegex = table.Column<string>(type: "TEXT", nullable: true),
                    ExpenseCategoryID = table.Column<int>(type: "INTEGER", nullable: true)
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
                name: "IX_ExpenseCategoryRules_ExpenseCategoryID",
                table: "ExpenseCategoryRules",
                column: "ExpenseCategoryID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseCategoryRules");
        }
    }
}
