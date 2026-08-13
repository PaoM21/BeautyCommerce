using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantSkuBarcodeUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Barcode",
                table: "ProductVariants",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SKU",
                table: "ProductVariants",
                column: "SKU",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_Barcode_NotBlank",
                table: "ProductVariants",
                sql: "length(trim(\"Barcode\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_SKU_NotBlank",
                table: "ProductVariants",
                sql: "length(trim(\"SKU\")) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_Barcode",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_SKU",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_Barcode_NotBlank",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_SKU_NotBlank",
                table: "ProductVariants");
        }
    }
}
