using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddGatewayRoutingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GatewayChannel",
                table: "BotIntegrations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayExternalId",
                table: "BotIntegrations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableGatewayRouting",
                table: "Behaviors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GatewayCaseSensitive",
                table: "Behaviors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GatewayMatchType",
                table: "Behaviors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GatewayTriggerPhrases",
                table: "Behaviors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "GatewayChannel",
                table: "BotIntegrations");

            migrationBuilder.DropColumn(
                name: "GatewayExternalId",
                table: "BotIntegrations");

            migrationBuilder.DropColumn(
                name: "EnableGatewayRouting",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "GatewayCaseSensitive",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "GatewayMatchType",
                table: "Behaviors");

            migrationBuilder.DropColumn(
                name: "GatewayTriggerPhrases",
                table: "Behaviors");
        }
    }
}
