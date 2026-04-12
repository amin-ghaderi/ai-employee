using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeBotIntegrationChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite: unique index is on (Channel, ExternalId). Variants like "telegram" vs "telegram "
            // become duplicates once Channel is normalized — remove extras before UPDATE.
            migrationBuilder.Sql(
                """
                DELETE FROM BotIntegrations
                WHERE rowid NOT IN (
                    SELECT MIN(rowid)
                    FROM BotIntegrations
                    GROUP BY LOWER(TRIM(Channel)), ExternalId
                );

                UPDATE BotIntegrations
                SET Channel = LOWER(TRIM(Channel))
                WHERE Channel IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data normalization is not reversible.
        }
    }
}
