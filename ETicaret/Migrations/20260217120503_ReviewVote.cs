using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETicaret.Migrations
{
    /// <inheritdoc />
    public partial class ReviewVote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReviewVotes",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserReviewId = table.Column<int>(type: "integer", nullable: false),
                    VoteType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReviewVotes", x => new { x.UserId, x.UserReviewId });
                    table.ForeignKey(
                        name: "FK_UserReviewVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserReviewVotes_UserReviews_UserReviewId",
                        column: x => x.UserReviewId,
                        principalTable: "UserReviews",
                        principalColumn: "UserReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserReviewVotes_UserId",
                table: "UserReviewVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviewVotes_UserReviewId",
                table: "UserReviewVotes",
                column: "UserReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReviewVotes");
        }
    }
}
