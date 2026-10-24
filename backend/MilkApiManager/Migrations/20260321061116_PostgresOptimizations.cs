using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MilkApiManager.Migrations
{
    /// <inheritdoc />
    public partial class PostgresOptimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncOutboxEntries_CreatedAt",
                table: "SyncOutboxEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.Sql("""
                ALTER TABLE "AuditLogs"
                ALTER COLUMN "DetailsJson" TYPE jsonb
                USING "DetailsJson"::jsonb;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutboxEntries_CreatedAt",
                table: "SyncOutboxEntries",
                column: "CreatedAt",
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp")
                .Annotation("Npgsql:IndexMethod", "BRIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncOutboxEntries_CreatedAt",
                table: "SyncOutboxEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.Sql("""
                ALTER TABLE "AuditLogs"
                ALTER COLUMN "DetailsJson" TYPE text
                USING "DetailsJson"::text;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutboxEntries_CreatedAt",
                table: "SyncOutboxEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }
    }
}
