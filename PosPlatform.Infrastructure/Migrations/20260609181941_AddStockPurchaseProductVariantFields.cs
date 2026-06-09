using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPurchaseProductVariantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "StockPurchaseItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantBarcode",
                table: "StockPurchaseItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantColor",
                table: "StockPurchaseItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantName",
                table: "StockPurchaseItems",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantSKU",
                table: "StockPurchaseItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantSize",
                table: "StockPurchaseItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseItems_ProductVariantId",
                table: "StockPurchaseItems",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockPurchaseItems_ProductVariants_ProductVariantId",
                table: "StockPurchaseItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockPurchaseItems_ProductVariants_ProductVariantId",
                table: "StockPurchaseItems");

            migrationBuilder.DropIndex(
                name: "IX_StockPurchaseItems_ProductVariantId",
                table: "StockPurchaseItems");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "StockPurchaseItems");

            migrationBuilder.DropColumn(
                name: "VariantBarcode",
                table: "StockPurchaseItems");

            migrationBuilder.DropColumn(
                name: "VariantColor",
                table: "StockPurchaseItems");

            migrationBuilder.DropColumn(
                name: "VariantName",
                table: "StockPurchaseItems");

            migrationBuilder.DropColumn(
                name: "VariantSKU",
                table: "StockPurchaseItems");

            migrationBuilder.DropColumn(
                name: "VariantSize",
                table: "StockPurchaseItems");
        }
    }
}
