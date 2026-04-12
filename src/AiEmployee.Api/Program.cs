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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection(WhatsAppSettings.SectionName));
builder.Services.Configure<SlackSettings>(
    builder.Configuration.GetSection(SlackSettings.SectionName));
builder.Services.AddScoped<IActiveTelegramBotContext, ActiveTelegramBotContext>();
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));
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
builder.Services.AddHttpClient<IAiClient, AiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient<ITelegramClient, TelegramClient>();
builder.Services.AddHttpClient<ITelegramWebhookApiClient, TelegramWebhookApiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHttpClient("WhatsApp", client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHttpClient<SlackMessageSender>(client =>
{
    client.BaseAddress = new Uri("https://slack.com/api/");
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddScoped<ITelegramWebhookApplicationService, TelegramWebhookApplicationService>();
builder.Services.AddScoped<IIntegrationProvider, TelegramIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProvider, GenericWebhookIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProvider, WhatsAppIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProvider, SlackIntegrationProvider>();
builder.Services.AddScoped<IIntegrationProviderRegistry, IntegrationProviderRegistry>();
builder.Services.AddScoped<IIntegrationWebhookApplicationService, IntegrationWebhookApplicationService>();
builder.Services.AddDbContext<AiEmployeeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<EfConversationRepository>();
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
app.MapControllers();

app.Run();
