using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations;

/// <inheritdoc />
public partial class _20261124224437_AddReceiptDescription : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            schema: "SimplyBudget",
            table: "Receipt",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Description",
            schema: "SimplyBudget",
            table: "Receipt");
    }
}
