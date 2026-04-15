using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddPersonaPromptExtensionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChatOutputSchemaJson",
                table: "Personas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JudgeInstruction",
                table: "Personas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JudgeSchemaJson",
                table: "Personas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadInstruction",
                table: "Personas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadSchemaJson",
                table: "Personas",
                type: "text",
                nullable: true);

            // Staged rollout: copy judge/lead instruction + schema from the first linked Behavior per Persona (via Bots).
            migrationBuilder.Sql(
                """
                UPDATE "Personas" AS p
                SET
                  "JudgeInstruction" = s."JudgeInstruction",
                  "JudgeSchemaJson" = s."JudgeSchemaJson",
                  "LeadInstruction" = s."LeadInstruction",
                  "LeadSchemaJson" = s."LeadSchemaJson"
                FROM (
                  SELECT DISTINCT ON (bot."PersonaId")
                    bot."PersonaId" AS "Pid",
                    beh."JudgeInstruction",
                    beh."JudgeSchemaJson",
                    beh."LeadInstruction",
                    beh."LeadSchemaJson"
                  FROM "Bots" AS bot
                  INNER JOIN "Behaviors" AS beh ON beh."Id" = bot."BehaviorId"
                  ORDER BY bot."PersonaId", bot."Id"
                ) AS s
                WHERE p."Id" = s."Pid"
                  AND (
                    NULLIF(trim(s."JudgeInstruction"), '') IS NOT NULL
                    OR NULLIF(trim(s."JudgeSchemaJson"), '') IS NOT NULL
                    OR NULLIF(trim(s."LeadInstruction"), '') IS NOT NULL
                    OR NULLIF(trim(s."LeadSchemaJson"), '') IS NOT NULL
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatOutputSchemaJson",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "JudgeInstruction",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "JudgeSchemaJson",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "LeadInstruction",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "LeadSchemaJson",
                table: "Personas");
        }
    }
}

