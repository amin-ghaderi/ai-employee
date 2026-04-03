using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Telegram;

public class TelegramClient : ITelegramClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<TelegramSettings> _options;
    private readonly ILogger<TelegramClient> _logger;

    public TelegramClient(
        HttpClient httpClient,
        IOptions<TelegramSettings> options,
        ILogger<TelegramClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        // Telegram uses negative chat_id for groups and supergroups (e.g. -100…); only DMs are positive.
        if (chatId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chatId), "chatId cannot be zero.");
        }

        _logger.LogInformation("Sending to chatId: {ChatId}", chatId);

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

        _logger.LogInformation(
            "telegram_sendMessage_start | chatId={ChatId} textLength={TextLength} textPreview={TextPreview}",
            chatId,
            text.Length,
            text.Length <= 120 ? text : text[..120] + "…");

        var response = await _httpClient.PostAsJsonAsync(url, new { chat_id = chatId, text });

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "telegram_sendMessage_done | chatId={ChatId} statusCode={StatusCode} body={Body}",
            chatId,
            (int)response.StatusCode,
            responseBody.Length > 2000 ? responseBody[..2000] + "…" : responseBody);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Telegram sendMessage failed: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {responseBody}");
        }
    }
}
