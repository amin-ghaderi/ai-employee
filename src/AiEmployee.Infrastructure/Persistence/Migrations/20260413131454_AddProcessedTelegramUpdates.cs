using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedTelegramUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedTelegramUpdates",
                columns: table => new
                {
                    BotScopeKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TelegramUpdateId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
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
