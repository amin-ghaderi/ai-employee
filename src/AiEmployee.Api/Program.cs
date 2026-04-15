using AiEmployee.Application.Admin;
using AiEmployee.Application.Bots;
using AiEmployee.Application.Behaviors;
using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Integrations;
using AiEmployee.Application.Integrations.Providers;
using AiEmployee.Application.Messaging;
using AiEmployee.Application.Options;
using AiEmployee.Application.Personas;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.Services;
using AiEmployee.Application.UseCases;
using AiEmployee.Infrastructure.AI;
using AiEmployee.Infrastructure.News;
using AiEmployee.Infrastructure.BotConfig;
using AiEmployee.Infrastructure.Messaging;
using AiEmployee.Infrastructure.Persistence;
using AiEmployee.Infrastructure.Repositories;
using AiEmployee.Infrastructure.Options;
using AiEmployee.Api.Middleware;
using AiEmployee.Application.Telegram;
using AiEmployee.Infrastructure.Integrations.GenericWebhook;
using AiEmployee.Infrastructure.Integrations.Telegram;
using AiEmployee.Infrastructure.Integrations.Slack;
using AiEmployee.Infrastructure.Integrations.WhatsApp;
using AiEmployee.Infrastructure.Telegram;
using AiEmployee.Api.Gateway;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. PostgreSQL (Npgsql) is required.");

RegisterOpenTelemetry(builder);

builder.Services.AddFeatureManagement();
builder.Services.Configure<GatewayOptions>(
    builder.Configuration.GetSection(GatewayOptions.SectionName));
builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection(WhatsAppSettings.SectionName));
builder.Services.Configure<SlackSettings>(
    builder.Configuration.GetSection(SlackSettings.SectionName));
builder.Services.AddScoped<IActiveTelegramBotContext, ActiveTelegramBotContext>();
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.Configure<RagOptions>(
    builder.Configuration.GetSection(RagOptions.SectionName));
builder.Services.Configure<EmbeddingOptions>(
    builder.Configuration.GetSection(EmbeddingOptions.SectionName));
builder.Services.Configure<LiveNewsOptions>(
    builder.Configuration.GetSection(LiveNewsOptions.SectionName));
builder.Services.Configure<AppOptions>(
    builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISystemSettingsRepository, EfSystemSettingsRepository>();
builder.Services.AddScoped<IPublicBaseUrlProvider, CachingPublicBaseUrlProvider>();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<IChatOutputSchemaValidator, JsonSchemaChatOutputValidator>();
builder.Services.AddHttpClient<IAiClient, AiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
}).AddPolicyHandler(HttpRetryPolicy());
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
}).AddPolicyHandler(HttpRetryPolicy());
builder.Services.AddHttpClient(GoogleNewsRssService.HttpClientName, (sp, client) =>
{
    var o = sp.GetRequiredService<IOptions<LiveNewsOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(o.RequestTimeoutSeconds, 5, 120));
    var ua = string.IsNullOrWhiteSpace(o.UserAgent) ? "AiEmployee/1.0" : o.UserAgent.Trim();
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);
}).AddPolicyHandler(HttpRetryPolicy());
builder.Services.AddScoped<INewsSearchService, GoogleNewsRssService>();
builder.Services.AddHttpClient<ITelegramClient, TelegramClient>()
    .AddPolicyHandler(HttpRetryPolicy());
builder.Services.AddHttpClient<ITelegramWebhookApiClient, TelegramWebhookApiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
}).AddPolicyHandler(HttpRetryPolicy());
builder.Services.AddHttpClient("WhatsApp", client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
}).AddPolicyHandler(HttpRetryPolicy());
builder.Services.AddHttpClient<SlackMessageSender>(client =>
{
    client.BaseAddress = new Uri("https://slack.com/api/");
    client.Timeout = TimeSpan.FromMinutes(2);
}).AddPolicyHandler(HttpRetryPolicy());
builder.Services.AddScoped<ITelegramWebhookApplicationService, TelegramWebhookApplicationService>();
builder.Services.AddScoped<IIntegrationProvider, TelegramIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProvider, GenericWebhookIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProvider, WhatsAppIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProvider, SlackIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProviderRegistry, IntegrationProviderRegistry>();
builder.Services.AddScoped<IIntegrationWebhookApplicationService, IntegrationWebhookApplicationService>();

builder.Services.AddSingleton<MessageEmbeddingWorkQueue>();
builder.Services.AddSingleton<IMessageEmbeddingPublisher, ChannelMessageEmbeddingPublisher>();
builder.Services.AddHostedService<MessageEmbeddingIndexingBackgroundService>();
builder.Services.AddScoped<IVectorStore, PgVectorStore>();

builder.Services.AddDbContext<AiEmployeeDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Postgres", "public");
        npgsql.CommandTimeout(60);
        npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null);
        npgsql.UseVector();
    });
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<AiEmployeeDbContext>(
        name: "database",
        tags: ["ready"]);

builder.Services.AddSingleton<IGatewayTelemetry, GatewayTelemetryRecorder>();
builder.Services.AddScoped<EfConversationRepository>();
builder.Services.AddScoped<ITelegramUpdateDeduplicator, EfTelegramUpdateDeduplicator>();
builder.Services.AddScoped<IConversationRepository>(sp =>
    new TestScopedConversationRepository(
        sp.GetRequiredService<EfConversationRepository>(),
        sp.GetRequiredService<RealFlowTestContext>()));
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ILeadRepository, EfLeadRepository>();
builder.Services.AddScoped<IJudgmentRepository, EfJudgmentRepository>();
builder.Services.AddScoped<IBotConfigurationRepository, EfBotConfigurationRepository>();
builder.Services.AddScoped<IBotConfigurationCommand, EfBotConfigurationCommand>();
builder.Services.AddScoped<IBehaviorRepository, EfBehaviorRepository>();
builder.Services.AddScoped<IBehaviorAdminService, BehaviorAdminService>();
builder.Services.AddScoped<IBotRepository, EfBotRepository>();
builder.Services.AddScoped<IBotAdminService, BotAdminService>();
builder.Services.AddScoped<IBotIntegrationRepository, EfBotIntegrationRepository>();
builder.Services.AddScoped<IBotIntegrationAdminService, BotIntegrationAdminService>();
builder.Services.AddScoped<IPersonaRepository, EfPersonaRepository>();
builder.Services.AddScoped<IPromptVersionReadRepository, EfPromptVersionReadRepository>();
builder.Services.AddScoped<IPersonaAdminService, PersonaAdminService>();
builder.Services.AddScoped<ILanguageProfileRepository, EfLanguageProfileRepository>();
builder.Services.AddScoped<IAdminSettingsService, AdminSettingsService>();
builder.Services.AddScoped<IAdminConfigService, AdminConfigService>();
builder.Services.AddScoped<IAdminTestService, AdminTestService>();
builder.Services.AddScoped<IPromptDebugService, PromptDebugService>();
builder.Services.AddScoped<IJudgeExecutionService, JudgeExecutionService>();
builder.Services.AddScoped<ILeadExecutionService, LeadExecutionService>();
builder.Services.AddScoped<RealFlowTestService>();
builder.Services.AddScoped<IBotResolver, BotResolver>();
builder.Services.AddScoped<IChannelAdapter, TelegramChannelAdapter>();
builder.Services.AddScoped<IChannelMessageSender, TelegramMessageSender>();
builder.Services.AddScoped<IChannelMessageSender, SlackMessageSender>();
builder.Services.AddScoped<RealFlowTestContext>();
builder.Services.AddScoped<OutgoingMessageDispatcher>();
builder.Services.AddScoped<IGatewayPhraseEvaluator, GatewayPhraseEvaluator>();
builder.Services.AddScoped<GatewayDispatcher>();
builder.Services.AddScoped<IGatewayDispatcher, GatewayDispatchFeatureDecorator>();
builder.Services.AddScoped<IOutgoingMessageClient>(sp =>
    new CapturingOutgoingClientDecorator(
        sp.GetRequiredService<OutgoingMessageDispatcher>(),
        sp.GetRequiredService<RealFlowTestContext>()));
builder.Services.AddScoped<IFlowTracker, FlowTracker>();
builder.Services.AddScoped<IIncomingMessageHandler, IncomingMessageHandler>();
builder.Services.AddScoped<AutomationService>();
builder.Services.AddScoped<UserTaggingService>();
builder.Services.AddScoped<LeadClassificationService>();
builder.Services.AddSingleton<BehaviorPromptMapper>();
builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<PromptComposer>();
builder.Services.AddScoped<JudgeUseCase>();
builder.Services.AddScoped<AssistantUseCase>();
builder.Services.AddScoped<BotConfigurationSeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiEmployeeDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<BotConfigurationSeeder>();
    await seeder.SeedAsync();

    var tg = scope.ServiceProvider.GetRequiredService<IOptions<TelegramSettings>>().Value;
    var startupLog = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Telegram.Configuration");
    startupLog.LogInformation(
        "Telegram BotToken: {Summary}",
        TelegramTokenUtilities.DescribeForLog(tg.BotToken));

    var publicBaseUrlProvider = scope.ServiceProvider.GetRequiredService<IPublicBaseUrlProvider>();
    var publicBaseUrl = publicBaseUrlProvider.GetPublicBaseUrl();
    var appStartupLog = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("App.Configuration");
    appStartupLog.LogInformation(
        "App:PublicBaseUrl: {Status}",
        string.IsNullOrEmpty(publicBaseUrl)
            ? "not set (set App:PublicBaseUrl for automatic Telegram webhook URLs)"
            : "configured");
}

app.UseRouting();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();
app.UseMiddleware<AdminAuthMiddleware>();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live"),
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
});
app.MapControllers();

app.Run();

static void RegisterOpenTelemetry(WebApplicationBuilder builder)
{
    var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"]
        ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

    if (string.IsNullOrWhiteSpace(otlpEndpoint))
        return;

    if (!Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpointUri))
        return;

    var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
            serviceName: "AiEmployee.Api",
            serviceVersion: version))
        .WithTracing(tracing => tracing
            .AddSource(AiClientTelemetry.ActivitySource.Name)
            .AddSource(GatewayTelemetryPrimitives.ActivitySource.Name)
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(options => options.Endpoint = endpointUri))
        .WithMetrics(metrics => metrics
            .AddMeter(AiClientTelemetry.Meter.Name)
            .AddMeter(GatewayTelemetryPrimitives.Meter.Name)
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(options => options.Endpoint = endpointUri));
}

static IAsyncPolicy<HttpResponseMessage> HttpRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(m => m.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)));
