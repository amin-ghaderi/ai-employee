using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBehaviorPromptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JudgeInstruction",
                table: "Behaviors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JudgeSchemaJson",
                table: "Behaviors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadInstruction",
                table: "Behaviors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadSchemaJson",
                table: "Behaviors",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
