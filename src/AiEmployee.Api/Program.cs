using AiEmployee.Application.UseCases;
using AiEmployee.Domain.Interfaces;
using AiEmployee.Domain.Services;
using AiEmployee.Infrastructure.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<IAiClient, AiClient>();
builder.Services.AddScoped<JudgeService>();
builder.Services.AddScoped<HandleMessageUseCase>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
