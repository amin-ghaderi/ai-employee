using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEmployee.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialPostgresBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Behaviors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JudgeContextMessageCount = table.Column<int>(type: "integer", nullable: false),
                    JudgePerMessageMaxChars = table.Column<int>(type: "integer", nullable: false),
                    JudgeCommandPrefix = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExcludeCommandsFromJudgeContext = table.Column<bool>(type: "boolean", nullable: false),
                    OnboardingFirstMessageOnly = table.Column<bool>(type: "boolean", nullable: false),
                    LeadFollowUpIndex = table.Column<int>(type: "integer", nullable: true),
                    LeadCaptureIndex = table.Column<int>(type: "integer", nullable: true),
                    AnswerKeysJson = table.Column<string>(type: "text", nullable: false),
                    AutomationRulesJson = table.Column<string>(type: "text", nullable: false),
                    EngagementNewUserWindowHours = table.Column<int>(type: "integer", nullable: false),
                    EngagementActiveMessageThreshold = table.Column<int>(type: "integer", nullable: false),
                    EngagementInactiveHoursThreshold = table.Column<int>(type: "integer", nullable: false),
                    EngagementHighEngagementScoreThreshold = table.Column<double>(type: "double precision", nullable: false),
                    EngagementNormalizationFactor = table.Column<double>(type: "double precision", nullable: false),
                    EngagementStickyTagsJson = table.Column<string>(type: "text", nullable: false),
                    HotLeadPotentialValue = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HotLeadTag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EnableChat = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableLead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableJudge = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    JudgeInstruction = table.Column<string>(type: "text", nullable: true),
                    JudgeSchemaJson = table.Column<string>(type: "text", nullable: true),
                    LeadInstruction = table.Column<string>(type: "text", nullable: true),
                    LeadSchemaJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Behaviors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LanguageProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Formality = table.Column<int>(type: "integer", nullable: false),
                    OnboardingGoalQuestion = table.Column<string>(type: "text", nullable: false),
                    ExperienceFollowUpQuestion = table.Column<string>(type: "text", nullable: false),
                    LeadThanksMessage = table.Column<string>(type: "text", nullable: false),
                    JudgeNoConversationMessage = table.Column<string>(type: "text", nullable: false),
                    JudgeNotEnoughContextMessage = table.Column<string>(type: "text", nullable: false),
                    JudgeResultTemplate = table.Column<string>(type: "text", nullable: false),
                    GenericErrorMessage = table.Column<string>(type: "text", nullable: false),
                    ReactivationMessage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    JudgePrompt = table.Column<string>(type: "text", nullable: false),
                    LeadPrompt = table.Column<string>(type: "text", nullable: false),
                    ClassificationSchemaJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Template = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessagesCount = table.Column<int>(type: "integer", nullable: false),
                    EngagementScore = table.Column<double>(type: "double precision", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    ExternalIntegrationId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PersonaId = table.Column<Guid>(type: "uuid", nullable: false),
                    BehaviorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bots_Behaviors_BehaviorId",
                        column: x => x.BehaviorId,
                        principalTable: "Behaviors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bots_LanguageProfiles_LanguageProfileId",
                        column: x => x.LanguageProfileId,
                        principalTable: "LanguageProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bots_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromptVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptType = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptVersions_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Judgments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    InputText = table.Column<string>(type: "text", nullable: false),
                    Winner = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Judgments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Judgments_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Judgments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Answers = table.Column<string>(type: "text", nullable: false),
                    UserType = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "text", nullable: false),
                    Potential = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotIntegrations_Bots_BotId",
                        column: x => x.BotId,
                        principalTable: "Bots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotIntegrations_BotId",
                table: "BotIntegrations",
                column: "BotId");

            migrationBuilder.CreateIndex(
                name: "IX_BotIntegrations_Channel_ExternalId",
                table: "BotIntegrations",
                columns: new[] { "Channel", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bots_BehaviorId",
                table: "Bots",
                column: "BehaviorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bots_Channel_ExternalIntegrationId",
                table: "Bots",
                columns: new[] { "Channel", "ExternalIntegrationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bots_LanguageProfileId",
                table: "Bots",
                column: "LanguageProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Bots_PersonaId",
                table: "Bots",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Judgments_ConversationId",
                table: "Judgments",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Judgments_UserId",
                table: "Judgments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageProfiles_Locale",
                table: "LanguageProfiles",
                column: "Locale");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_UserId",
                table: "Leads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_Name",
                table: "PromptTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromptVersions_PersonaId_PromptType_Version",
                table: "PromptVersions",
                columns: new[] { "PersonaId", "PromptType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotIntegrations");

            migrationBuilder.DropTable(
                name: "Judgments");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "PromptVersions");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Bots");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Behaviors");

            migrationBuilder.DropTable(
                name: "LanguageProfiles");

            migrationBuilder.DropTable(
                name: "Personas");
        }
    }
}

