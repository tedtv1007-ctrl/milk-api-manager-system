using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MilkApiManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncOutboxEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncOutboxEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOutboxEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutboxEntries_CreatedAt",
                table: "SyncOutboxEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutboxEntries_Status_NextAttemptAt",
                table: "SyncOutboxEntries",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncOutboxEntries");
        }
    }
}
