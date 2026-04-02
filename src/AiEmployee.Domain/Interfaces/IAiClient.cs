namespace AiEmployee.Domain.Interfaces;

public interface IAiClient
{
    Task<string> AskAsync(string prompt);
}
