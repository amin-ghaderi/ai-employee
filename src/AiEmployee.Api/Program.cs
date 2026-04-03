using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Services;
using AiEmployee.Application.UseCases;
using AiEmployee.Infrastructure.AI;
using AiEmployee.Infrastructure.Repositories;
using AiEmployee.Infrastructure.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("Telegram"));

builder.Services.AddControllers();
builder.Services.AddHttpClient<IAiClient, AiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient<ITelegramClient, TelegramClient>();
builder.Services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();
builder.Services.AddScoped<AutomationService>();
builder.Services.AddScoped<LeadClassificationService>();
builder.Services.AddScoped<JudgeUseCase>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
