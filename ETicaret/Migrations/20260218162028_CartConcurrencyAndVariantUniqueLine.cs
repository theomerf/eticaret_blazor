using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETicaret.Migrations
{
    /// <inheritdoc />
    public partial class CartConcurrencyAndVariantUniqueLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_Coupon_User",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_CartLines_CartId",
                table: "CartLines");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ProductVariants",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Coupons",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_Coupon_User",
                table: "CouponUsages",
                columns: new[] { "CouponId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartLines_CartId_ProductVariantId",
                table: "CartLines",
                columns: new[] { "CartId", "ProductVariantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_Coupon_User",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_CartLines_CartId_ProductVariantId",
                table: "CartLines");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Coupons");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_Coupon_User",
                table: "CouponUsages",
                columns: new[] { "CouponId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CartLines_CartId",
                table: "CartLines",
                column: "CartId");
        }
    }
}
