using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LlmTornado.Internal.Press.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    IdeaSummary = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ArticleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedRelevance = table.Column<double>(type: "REAL", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ImageVariationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Objective = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    QualityScore = table.Column<double>(type: "REAL", nullable: false),
                    IterationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: true),
                    SourcesJson = table.Column<string>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrendingTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Topic = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Relevance = table.Column<double>(type: "REAL", nullable: false),
                    DiscoveredDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArticleCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrendingTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArticleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    AgentName = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    DataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkHistory_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleQueue_CreatedDate",
                table: "ArticleQueue",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleQueue_Priority",
                table: "ArticleQueue",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleQueue_ScheduledDate",
                table: "ArticleQueue",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleQueue_Status",
                table: "ArticleQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CreatedDate",
                table: "Articles",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Slug",
                table: "Articles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Status",
                table: "Articles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrendingTopics_DiscoveredDate",
                table: "TrendingTopics",
                column: "DiscoveredDate");

            migrationBuilder.CreateIndex(
                name: "IX_TrendingTopics_IsActive",
                table: "TrendingTopics",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrendingTopics_Relevance",
                table: "TrendingTopics",
                column: "Relevance");

            migrationBuilder.CreateIndex(
                name: "IX_TrendingTopics_Topic",
                table: "TrendingTopics",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_WorkHistory_Action",
                table: "WorkHistory",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_WorkHistory_ArticleId",
                table: "WorkHistory",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkHistory_Timestamp",
                table: "WorkHistory",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleQueue");

            migrationBuilder.DropTable(
                name: "TrendingTopics");

            migrationBuilder.DropTable(
                name: "WorkHistory");

            migrationBuilder.DropTable(
                name: "Articles");
        }
    }
}
