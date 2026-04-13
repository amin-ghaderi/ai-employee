using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddProcessedTelegramUpdatesPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedTelegramUpdates",
                columns: table => new
                {
                    BotScopeKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TelegramUpdateId = table.Column<long>(type: "bigint", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedTelegramUpdates", x => new { x.BotScopeKey, x.TelegramUpdateId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedTelegramUpdates");
        }
    }
}
