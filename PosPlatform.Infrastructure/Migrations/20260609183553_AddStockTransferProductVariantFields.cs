using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferProductVariantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceProductVariantId",
                table: "StockTransferItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetProductVariantId",
                table: "StockTransferItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantBarcode",
                table: "StockTransferItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantColor",
                table: "StockTransferItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantName",
                table: "StockTransferItems",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantSKU",
                table: "StockTransferItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantSize",
                table: "StockTransferItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_SourceProductVariantId",
                table: "StockTransferItems",
                column: "SourceProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_TargetProductVariantId",
                table: "StockTransferItems",
                column: "TargetProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferItems_ProductVariants_SourceProductVariantId",
                table: "StockTransferItems",
                column: "SourceProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferItems_ProductVariants_TargetProductVariantId",
                table: "StockTransferItems",
                column: "TargetProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferItems_ProductVariants_SourceProductVariantId",
                table: "StockTransferItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferItems_ProductVariants_TargetProductVariantId",
                table: "StockTransferItems");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferItems_SourceProductVariantId",
                table: "StockTransferItems");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferItems_TargetProductVariantId",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "SourceProductVariantId",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "TargetProductVariantId",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "VariantBarcode",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "VariantColor",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "VariantName",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "VariantSKU",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "VariantSize",
                table: "StockTransferItems");
        }
    }
}
