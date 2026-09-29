using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyBudgetWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Receipt",
                schema: "SimplyBudget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlobName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MerchantName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmountCents = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProcessingMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptExpenseLink",
                schema: "SimplyBudget",
                columns: table => new
                {
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    ExpenseCategoryItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptExpenseLink", x => x.ReceiptId);
                    table.ForeignKey(
                        name: "FK_ReceiptExpenseLink_ExpenseCategoryItem_ExpenseCategoryItemId",
                        column: x => x.ExpenseCategoryItemId,
                        principalSchema: "SimplyBudget",
                        principalTable: "ExpenseCategoryItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceiptExpenseLink_Receipt_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "SimplyBudget",
                        principalTable: "Receipt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptLineItem",
                schema: "SimplyBudget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AmountCents = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptLineItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptLineItem_Receipt_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "SimplyBudget",
                        principalTable: "Receipt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_TotalAmountCents_TransactionDate",
                schema: "SimplyBudget",
                table: "Receipt",
                columns: new[] { "TotalAmountCents", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptExpenseLink_ExpenseCategoryItemId",
                schema: "SimplyBudget",
                table: "ReceiptExpenseLink",
                column: "ExpenseCategoryItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptLineItem_ReceiptId",
                schema: "SimplyBudget",
                table: "ReceiptLineItem",
                column: "ReceiptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptExpenseLink",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "ReceiptLineItem",
                schema: "SimplyBudget");

            migrationBuilder.DropTable(
                name: "Receipt",
                schema: "SimplyBudget");
        }
    }
}
