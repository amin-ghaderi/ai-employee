using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.Services;
using AiEmployee.Application.UseCases;
using AiEmployee.Infrastructure.AI;
using AiEmployee.Infrastructure.BotConfig;
using AiEmployee.Infrastructure.Messaging;
using AiEmployee.Infrastructure.Persistence;
using AiEmployee.Infrastructure.Repositories;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddHttpClient<IAiClient, AiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient<ITelegramClient, TelegramClient>();
builder.Services.AddDbContext<AiEmployeeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IConversationRepository, EfConversationRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ILeadRepository, EfLeadRepository>();
builder.Services.AddScoped<IJudgmentRepository, EfJudgmentRepository>();
builder.Services.AddScoped<IBotConfigurationRepository, EfBotConfigurationRepository>();
builder.Services.AddScoped<IBotResolver, BotResolver>();
builder.Services.AddScoped<IChannelAdapter, TelegramChannelAdapter>();
builder.Services.AddScoped<IChannelMessageSender, TelegramMessageSender>();
builder.Services.AddScoped<IOutgoingMessageClient, OutgoingMessageDispatcher>();
builder.Services.AddScoped<IIncomingMessageHandler, IncomingMessageHandler>();
builder.Services.AddScoped<AutomationService>();
builder.Services.AddScoped<UserTaggingService>();
builder.Services.AddScoped<LeadClassificationService>();
builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddScoped<JudgeUseCase>();
builder.Services.AddScoped<BotConfigurationSeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiEmployeeDbContext>();
    await db.Database.MigrateAsync();
    var seeder = scope.ServiceProvider.GetRequiredService<BotConfigurationSeeder>();
    await seeder.SeedAsync();
}

app.UseRouting();
app.MapControllers();

app.Run();
