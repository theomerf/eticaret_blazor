using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace ETicaret.Migrations
{
    /// <inheritdoc />
    public partial class Dashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReviews_Product_Approved",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_Filtered",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductImage_ProductId_Primary_Filtered",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_Payment",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_User_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Scheduled",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_User_Unread",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_Active_Dates",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_IsDeleted_Filtered",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsDeleted_Filtered",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsVisible_Order",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_Active_Dates",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_IsDeleted_Filtered",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_Scope_Active",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_IsDeleted_Filtered",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Products",
                type: "tsvector",
                nullable: false,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector")
                .Annotation("Npgsql:TsVectorConfig", "turkish")
                .Annotation("Npgsql:TsVectorProperties", new[] { "ProductName", "Brand", "Summary", "LongDescription", "MetaTitle", "MetaDescription", "Gtin" });

            migrationBuilder.AlterColumn<string>(
                name: "LongDescription",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_Product_Approved",
                table: "UserReviews",
                columns: new[] { "ProductId", "IsApproved", "IsDeleted" },
                filter: "\"IsApproved\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_Filtered",
                table: "Products",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchVector",
                table: "Products",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImage_ProductId_Primary_Filtered",
                table: "ProductImages",
                columns: new[] { "ProductId", "IsPrimary" },
                filter: "\"IsPrimary\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_Payment",
                table: "Orders",
                columns: new[] { "OrderStatus", "PaymentStatus", "IsDeleted" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_User_Status",
                table: "Orders",
                columns: new[] { "UserId", "OrderStatus", "IsDeleted" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Scheduled",
                table: "Notifications",
                columns: new[] { "ScheduledFor", "IsSent" },
                filter: "\"ScheduledFor\" IS NOT NULL AND \"IsSent\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_Unread",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "IsDeleted" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Active_Dates",
                table: "Coupons",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" },
                filter: "\"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IsDeleted_Filtered",
                table: "Coupons",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsDeleted_Filtered",
                table: "Categories",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsVisible_Order",
                table: "Categories",
                columns: new[] { "IsVisible", "DisplayOrder", "IsDeleted" },
                filter: "\"IsDeleted\" = false AND \"IsVisible\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Active_Dates",
                table: "Campaigns",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" },
                filter: "\"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_IsDeleted_Filtered",
                table: "Campaigns",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Scope_Active",
                table: "Campaigns",
                columns: new[] { "Scope", "IsActive" },
                filter: "\"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_IsDeleted_Filtered",
                table: "AspNetUsers",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses",
                columns: new[] { "UserId", "IsDefault", "IsDeleted" },
                filter: "\"IsDefault\" = true AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReviews_Product_Approved",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_Filtered",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SearchVector",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductImage_ProductId_Primary_Filtered",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_Payment",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_User_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Scheduled",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_User_Unread",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_Active_Dates",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_IsDeleted_Filtered",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsDeleted_Filtered",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsVisible_Order",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_Active_Dates",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_IsDeleted_Filtered",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_Scope_Active",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_IsDeleted_Filtered",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Products",
                type: "tsvector",
                nullable: false,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector")
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "ProductName", "Brand", "Summary", "LongDescription", "MetaTitle", "MetaDescription", "Gtin" });

            migrationBuilder.AlterColumn<string>(
                name: "LongDescription",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_Product_Approved",
                table: "UserReviews",
                columns: new[] { "ProductId", "IsApproved", "IsDeleted" },
                filter: "[IsApproved] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_Filtered",
                table: "Products",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImage_ProductId_Primary_Filtered",
                table: "ProductImages",
                columns: new[] { "ProductId", "IsPrimary" },
                filter: "[IsPrimary] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_Payment",
                table: "Orders",
                columns: new[] { "OrderStatus", "PaymentStatus", "IsDeleted" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_User_Status",
                table: "Orders",
                columns: new[] { "UserId", "OrderStatus", "IsDeleted" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Scheduled",
                table: "Notifications",
                columns: new[] { "ScheduledFor", "IsSent" },
                filter: "[ScheduledFor] IS NOT NULL AND [IsSent] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_Unread",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "IsDeleted" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Active_Dates",
                table: "Coupons",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IsDeleted_Filtered",
                table: "Coupons",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsDeleted_Filtered",
                table: "Categories",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsVisible_Order",
                table: "Categories",
                columns: new[] { "IsVisible", "DisplayOrder", "IsDeleted" },
                filter: "[IsDeleted] = 0 AND [IsVisible] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Active_Dates",
                table: "Campaigns",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_IsDeleted_Filtered",
                table: "Campaigns",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Scope_Active",
                table: "Campaigns",
                columns: new[] { "Scope", "IsActive" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_IsDeleted_Filtered",
                table: "AspNetUsers",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses",
                columns: new[] { "UserId", "IsDefault", "IsDeleted" },
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");
        }
    }
}
