using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptImageGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageGeometryJson",
                schema: "SimplyBudget",
                table: "Receipt",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageGeometryJson",
                schema: "SimplyBudget",
                table: "Receipt");
        }
    }
}
