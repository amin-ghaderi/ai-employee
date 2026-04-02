using AiEmployee.Application.Interfaces;
using AiEmployee.Application.UseCases;
using AiEmployee.Infrastructure.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<IAiClient, AiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddScoped<JudgeUseCase>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
