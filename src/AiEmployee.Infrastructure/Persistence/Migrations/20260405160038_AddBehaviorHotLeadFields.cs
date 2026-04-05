using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBehaviorHotLeadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HotLeadPotentialValue",
                table: "Behaviors",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "high");

            migrationBuilder.AddColumn<string>(
                name: "HotLeadTag",
                table: "Behaviors",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "hot_lead");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HotLeadPotentialValue",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "HotLeadTag",
                table: "Behaviors");
        }
    }
}
