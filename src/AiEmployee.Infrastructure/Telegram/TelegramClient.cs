using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Telegram;

public class TelegramClient : ITelegramClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<TelegramSettings> _options;

    public TelegramClient(HttpClient httpClient, IOptions<TelegramSettings> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        if (chatId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chatId), "chatId must be positive.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("text cannot be empty.", nameof(text));
        }

        var token = _options.Value.BotToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Telegram BotToken is not configured.");
        }

        var url = $"https://api.telegram.org/bot{token}/sendMessage";
        var response = await _httpClient.PostAsJsonAsync(url, new { chat_id = chatId, text });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Telegram sendMessage failed: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {body}");
        }
    }
}
