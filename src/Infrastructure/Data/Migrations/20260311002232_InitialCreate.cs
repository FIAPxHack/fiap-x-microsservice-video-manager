using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoManagerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_uploads",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ZipFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FrameCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_uploads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoUploads_Status",
                table: "video_uploads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VideoUploads_StoredFileName",
                table: "video_uploads",
                column: "StoredFileName");

            migrationBuilder.CreateIndex(
                name: "IX_VideoUploads_UserId",
                table: "video_uploads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoUploads_UserId_UploadedAt",
                table: "video_uploads",
                columns: new[] { "UserId", "UploadedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_uploads");
        }
    }
}
