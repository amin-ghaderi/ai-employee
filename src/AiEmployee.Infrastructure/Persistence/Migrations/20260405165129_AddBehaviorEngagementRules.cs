using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBehaviorEngagementRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EngagementActiveMessageThreshold",
                table: "Behaviors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<double>(
                name: "EngagementHighEngagementScoreThreshold",
                table: "Behaviors",
                type: "REAL",
                nullable: false,
                defaultValue: 0.7);

            migrationBuilder.AddColumn<int>(
                name: "EngagementInactiveHoursThreshold",
                table: "Behaviors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 72);

            migrationBuilder.AddColumn<int>(
                name: "EngagementNewUserWindowHours",
                table: "Behaviors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 48);

            migrationBuilder.AddColumn<double>(
                name: "EngagementNormalizationFactor",
                table: "Behaviors",
                type: "REAL",
                nullable: false,
                defaultValue: 20.0);

            migrationBuilder.AddColumn<string>(
                name: "EngagementStickyTagsJson",
                table: "Behaviors",
                type: "TEXT",
                nullable: false,
                defaultValue: "[\"inactive_notified\",\"high_engagement_notified\"]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngagementActiveMessageThreshold",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "EngagementHighEngagementScoreThreshold",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "EngagementInactiveHoursThreshold",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "EngagementNewUserWindowHours",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "EngagementNormalizationFactor",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "EngagementStickyTagsJson",
                table: "Behaviors");
        }
    }
}
