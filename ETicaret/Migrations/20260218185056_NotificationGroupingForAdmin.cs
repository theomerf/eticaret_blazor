using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETicaret.Migrations
{
    /// <inheritdoc />
    public partial class NotificationGroupingForAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationGroupId",
                table: "Notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SentToAllActiveUsers",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_GroupId",
                table: "Notifications",
                column: "NotificationGroupId",
                filter: "\"NotificationGroupId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_GroupId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NotificationGroupId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SentToAllActiveUsers",
                table: "Notifications");
        }
    }
}
