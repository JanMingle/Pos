using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductSkuBranchUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_SKU",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_BranchId_SKU",
                table: "Products",
                columns: new[] { "TenantId", "BranchId", "SKU" },
                unique: true,
                filter: "[BranchId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_BranchId_SKU",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_SKU",
                table: "Products",
                columns: new[] { "TenantId", "SKU" },
                unique: true);
        }
    }
}
