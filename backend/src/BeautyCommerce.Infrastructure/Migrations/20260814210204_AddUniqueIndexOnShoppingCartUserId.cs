using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnShoppingCartUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserId",
                table: "ShoppingCarts",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserId",
                table: "ShoppingCarts");
        }
    }
}
