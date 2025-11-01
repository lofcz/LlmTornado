using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LlmTornado.Internal.Press.Migrations
{
    /// <inheritdoc />
    public partial class AddArticlePublishStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticlePublishStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArticleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Platform = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedUrl = table.Column<string>(type: "TEXT", nullable: true),
                    PlatformArticleId = table.Column<string>(type: "TEXT", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAttemptDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticlePublishStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticlePublishStatus_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticlePublishStatus_ArticleId_Platform",
                table: "ArticlePublishStatus",
                columns: new[] { "ArticleId", "Platform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticlePublishStatus_PublishedDate",
                table: "ArticlePublishStatus",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ArticlePublishStatus_Status",
                table: "ArticlePublishStatus",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticlePublishStatus");
        }
    }
}
