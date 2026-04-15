using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class Phase5RemoveBehaviorPromptFieldsPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Personas" AS p
                SET
                  "JudgeInstruction" = (
                    SELECT b."JudgeInstruction" FROM "Bots" AS bot
                    INNER JOIN "Behaviors" AS b ON b."Id" = bot."BehaviorId"
                    WHERE bot."PersonaId" = p."Id"
                    ORDER BY bot."Id"
                    LIMIT 1
                  ),
                  "JudgeSchemaJson" = (
                    SELECT b."JudgeSchemaJson" FROM "Bots" AS bot
                    INNER JOIN "Behaviors" AS b ON b."Id" = bot."BehaviorId"
                    WHERE bot."PersonaId" = p."Id"
                    ORDER BY bot."Id"
                    LIMIT 1
                  )
                WHERE
                  (
                    (p."JudgeInstruction" IS NULL OR trim(p."JudgeInstruction") = '')
                    AND (
                      p."JudgeSchemaJson" IS NULL
                      OR trim(p."JudgeSchemaJson") = ''
                      OR lower(trim(p."JudgeSchemaJson")) = '{}'
                      OR lower(trim(p."JudgeSchemaJson")) = 'null'
                    )
                  )
                  AND EXISTS (
                    SELECT 1 FROM "Bots" AS bot
                    INNER JOIN "Behaviors" AS b ON b."Id" = bot."BehaviorId"
                    WHERE bot."PersonaId" = p."Id"
                      AND (
                        (b."JudgeInstruction" IS NOT NULL AND trim(b."JudgeInstruction") <> '')
                        OR (
                          b."JudgeSchemaJson" IS NOT NULL
                          AND trim(b."JudgeSchemaJson") <> ''
                          AND lower(trim(b."JudgeSchemaJson")) NOT IN ('{}', 'null')
                        )
                      )
                  );
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Personas" AS p
                SET
                  "LeadInstruction" = (
                    SELECT b."LeadInstruction" FROM "Bots" AS bot
                    INNER JOIN "Behaviors" AS b ON b."Id" = bot."BehaviorId"
                    WHERE bot."PersonaId" = p."Id"
                    ORDER BY bot."Id"
                    LIMIT 1
                  ),
                  "LeadSchemaJson" = (
                    SELECT b."LeadSchemaJson" FROM "Bots" AS bot
                    INNER JOIN "Behaviors" AS b ON b."Id" = bot."BehaviorId"
                    WHERE bot."PersonaId" = p."Id"
                    ORDER BY bot."Id"
                    LIMIT 1
                  )
                WHERE
                  (
                    (p."LeadInstruction" IS NULL OR trim(p."LeadInstruction") = '')
                    AND (
                      p."LeadSchemaJson" IS NULL
                      OR trim(p."LeadSchemaJson") = ''
                      OR lower(trim(p."LeadSchemaJson")) = '{}'
                      OR lower(trim(p."LeadSchemaJson")) = 'null'
                    )
                  )
                  AND EXISTS (
                    SELECT 1 FROM "Bots" AS bot
                    INNER JOIN "Behaviors" AS b ON b."Id" = bot."BehaviorId"
                    WHERE bot."PersonaId" = p."Id"
                      AND (
                        (b."LeadInstruction" IS NOT NULL AND trim(b."LeadInstruction") <> '')
                        OR (
                          b."LeadSchemaJson" IS NOT NULL
                          AND trim(b."LeadSchemaJson") <> ''
                          AND lower(trim(b."LeadSchemaJson")) NOT IN ('{}', 'null')
                        )
                      )
                  );
                """);

            migrationBuilder.DropColumn(
                name: "JudgeInstruction",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "JudgeSchemaJson",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "LeadInstruction",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "LeadSchemaJson",
                table: "Behaviors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JudgeInstruction",
                table: "Behaviors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JudgeSchemaJson",
                table: "Behaviors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadInstruction",
                table: "Behaviors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadSchemaJson",
                table: "Behaviors",
                type: "text",
                nullable: true);
        }
    }
}

