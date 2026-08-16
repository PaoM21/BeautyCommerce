using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ShoppingCartItems_Quantity_Positive",
                table: "ShoppingCartItems",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShoppingCartItems_UnitPrice_NonNegative",
                table: "ShoppingCartItems",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_Cost_NonNegative",
                table: "ProductVariants",
                sql: "\"Cost\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_MinimumStock_NonNegative",
                table: "ProductVariants",
                sql: "\"MinimumStock\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_OldPrice_NonNegative",
                table: "ProductVariants",
                sql: "\"OldPrice\" IS NULL OR \"OldPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_Price_NonNegative",
                table: "ProductVariants",
                sql: "\"Price\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_Stock_NonNegative",
                table: "ProductVariants",
                sql: "\"Stock\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_SubTotal_NonNegative",
                table: "Orders",
                sql: "\"SubTotal\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Total_NonNegative",
                table: "Orders",
                sql: "\"Total\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_UnitPrice_NonNegative",
                table: "OrderItems",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryMovements_Quantity_Positive",
                table: "InventoryMovements",
                sql: "\"Quantity\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ShoppingCartItems_Quantity_Positive",
                table: "ShoppingCartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShoppingCartItems_UnitPrice_NonNegative",
                table: "ShoppingCartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_Cost_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_MinimumStock_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_OldPrice_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_Price_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_Stock_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_SubTotal_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Total_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_UnitPrice_NonNegative",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovements_Quantity_Positive",
                table: "InventoryMovements");
        }
    }
}
