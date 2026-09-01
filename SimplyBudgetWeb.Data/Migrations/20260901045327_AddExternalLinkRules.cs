using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalLinkRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalLinkRule",
                schema: "SimplyBudget",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuleRegex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLinkRule", x => x.ID);
                });

            // Preserve the previously hard coded Amazon link as a rule.
            migrationBuilder.InsertData(
                schema: "SimplyBudget",
                table: "ExternalLinkRule",
                columns: ["Name", "RuleRegex", "Url"],
                values: ["Amazon", @"\bamazon\b", "https://www.amazon.com/cpe/yourpayments/transactions"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalLinkRule",
                schema: "SimplyBudget");
        }
    }
}
