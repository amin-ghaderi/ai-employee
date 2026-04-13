CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory_Postgres" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory_Postgres" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Behaviors" (
    "Id" uuid NOT NULL,
    "JudgeContextMessageCount" integer NOT NULL,
    "JudgePerMessageMaxChars" integer NOT NULL,
    "JudgeCommandPrefix" character varying(256) NOT NULL,
    "ExcludeCommandsFromJudgeContext" boolean NOT NULL,
    "OnboardingFirstMessageOnly" boolean NOT NULL,
    "LeadFollowUpIndex" integer,
    "LeadCaptureIndex" integer,
    "AnswerKeysJson" text NOT NULL,
    "AutomationRulesJson" text NOT NULL,
    "EngagementNewUserWindowHours" integer NOT NULL,
    "EngagementActiveMessageThreshold" integer NOT NULL,
    "EngagementInactiveHoursThreshold" integer NOT NULL,
    "EngagementHighEngagementScoreThreshold" double precision NOT NULL,
    "EngagementNormalizationFactor" double precision NOT NULL,
    "EngagementStickyTagsJson" text NOT NULL,
    "HotLeadPotentialValue" character varying(128) NOT NULL,
    "HotLeadTag" character varying(128) NOT NULL,
    "EnableChat" boolean NOT NULL DEFAULT TRUE,
    "EnableLead" boolean NOT NULL DEFAULT TRUE,
    "EnableJudge" boolean NOT NULL DEFAULT TRUE,
    "JudgeInstruction" text,
    "JudgeSchemaJson" text,
    "LeadInstruction" text,
    "LeadSchemaJson" text,
    CONSTRAINT "PK_Behaviors" PRIMARY KEY ("Id")
);

CREATE TABLE "Conversations" (
    "Id" text NOT NULL,
    CONSTRAINT "PK_Conversations" PRIMARY KEY ("Id")
);

CREATE TABLE "LanguageProfiles" (
    "Id" uuid NOT NULL,
    "Locale" character varying(256) NOT NULL,
    "Formality" integer NOT NULL,
    "OnboardingGoalQuestion" text NOT NULL,
    "ExperienceFollowUpQuestion" text NOT NULL,
    "LeadThanksMessage" text NOT NULL,
    "JudgeNoConversationMessage" text NOT NULL,
    "JudgeNotEnoughContextMessage" text NOT NULL,
    "JudgeResultTemplate" text NOT NULL,
    "GenericErrorMessage" text NOT NULL,
    "ReactivationMessage" text NOT NULL,
    CONSTRAINT "PK_LanguageProfiles" PRIMARY KEY ("Id")
);

CREATE TABLE "Personas" (
    "Id" uuid NOT NULL,
    "DisplayName" character varying(256) NOT NULL,
    "SystemPrompt" text NOT NULL,
    "JudgePrompt" text NOT NULL,
    "LeadPrompt" text NOT NULL,
    "ClassificationSchemaJson" text NOT NULL,
    CONSTRAINT "PK_Personas" PRIMARY KEY ("Id")
);

CREATE TABLE "PromptTemplates" (
    "Id" uuid NOT NULL,
    "Name" character varying(256) NOT NULL,
    "Template" text NOT NULL,
    CONSTRAINT "PK_PromptTemplates" PRIMARY KEY ("Id")
);

CREATE TABLE "SystemSettings" (
    "Id" uuid NOT NULL,
    "Key" character varying(128) NOT NULL,
    "Value" character varying(2048),
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SystemSettings" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" text NOT NULL,
    "Username" text,
    "FirstName" text,
    "LastName" text,
    "JoinedAt" timestamp with time zone NOT NULL,
    "LastActiveAt" timestamp with time zone NOT NULL,
    "MessagesCount" integer NOT NULL,
    "EngagementScore" double precision NOT NULL,
    "Tags" text NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "Messages" (
    "Id" uuid NOT NULL,
    "ConversationId" text NOT NULL,
    "UserId" text NOT NULL,
    "Username" text,
    "FirstName" text,
    "LastName" text,
    "Text" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Messages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Messages_Conversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Bots" (
    "Id" uuid NOT NULL,
    "Name" character varying(256) NOT NULL,
    "Channel" integer NOT NULL,
    "ExternalIntegrationId" character varying(512) NOT NULL,
    "PersonaId" uuid NOT NULL,
    "BehaviorId" uuid NOT NULL,
    "LanguageProfileId" uuid NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Bots" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Bots_Behaviors_BehaviorId" FOREIGN KEY ("BehaviorId") REFERENCES "Behaviors" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Bots_LanguageProfiles_LanguageProfileId" FOREIGN KEY ("LanguageProfileId") REFERENCES "LanguageProfiles" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Bots_Personas_PersonaId" FOREIGN KEY ("PersonaId") REFERENCES "Personas" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PromptVersions" (
    "Id" uuid NOT NULL,
    "PersonaId" uuid NOT NULL,
    "PromptType" integer NOT NULL,
    "Version" integer NOT NULL,
    "Content" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(256),
    CONSTRAINT "PK_PromptVersions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PromptVersions_Personas_PersonaId" FOREIGN KEY ("PersonaId") REFERENCES "Personas" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Judgments" (
    "Id" uuid NOT NULL,
    "ConversationId" text NOT NULL,
    "UserId" text NOT NULL,
    "InputText" text NOT NULL,
    "Winner" text NOT NULL,
    "Reason" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Judgments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Judgments_Conversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Judgments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Leads" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Answers" text NOT NULL,
    "UserType" text NOT NULL,
    "Intent" text NOT NULL,
    "Potential" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Leads" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Leads_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "BotIntegrations" (
    "Id" uuid NOT NULL,
    "BotId" uuid NOT NULL,
    "Channel" character varying(64) NOT NULL,
    "ExternalId" character varying(512) NOT NULL,
    "IsEnabled" boolean NOT NULL,
    CONSTRAINT "PK_BotIntegrations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BotIntegrations_Bots_BotId" FOREIGN KEY ("BotId") REFERENCES "Bots" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_BotIntegrations_BotId" ON "BotIntegrations" ("BotId");

CREATE UNIQUE INDEX "IX_BotIntegrations_Channel_ExternalId" ON "BotIntegrations" ("Channel", "ExternalId");

CREATE INDEX "IX_Bots_BehaviorId" ON "Bots" ("BehaviorId");

CREATE UNIQUE INDEX "IX_Bots_Channel_ExternalIntegrationId" ON "Bots" ("Channel", "ExternalIntegrationId");

CREATE INDEX "IX_Bots_LanguageProfileId" ON "Bots" ("LanguageProfileId");

CREATE INDEX "IX_Bots_PersonaId" ON "Bots" ("PersonaId");

CREATE INDEX "IX_Judgments_ConversationId" ON "Judgments" ("ConversationId");

CREATE INDEX "IX_Judgments_UserId" ON "Judgments" ("UserId");

CREATE INDEX "IX_LanguageProfiles_Locale" ON "LanguageProfiles" ("Locale");

CREATE INDEX "IX_Leads_UserId" ON "Leads" ("UserId");

CREATE INDEX "IX_Messages_ConversationId" ON "Messages" ("ConversationId");

CREATE UNIQUE INDEX "IX_PromptTemplates_Name" ON "PromptTemplates" ("Name");

CREATE UNIQUE INDEX "IX_PromptVersions_PersonaId_PromptType_Version" ON "PromptVersions" ("PersonaId", "PromptType", "Version");

CREATE UNIQUE INDEX "IX_SystemSettings_Key" ON "SystemSettings" ("Key");

INSERT INTO public."__EFMigrationsHistory_Postgres" ("MigrationId", "ProductVersion")
VALUES ('20260413093932_InitialPostgresBaseline', '10.0.4');

COMMIT;

