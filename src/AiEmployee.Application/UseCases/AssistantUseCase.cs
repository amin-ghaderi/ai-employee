using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.Rag;
using AiEmployee.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Application.UseCases;

public sealed class AssistantUseCase
{
    private readonly IAiClient _aiClient;
    private readonly PromptComposer _promptComposer;
    private readonly IConversationRepository _conversationRepository;
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly IOptions<RagOptions> _ragOptions;
    private readonly IOptions<EmbeddingOptions> _embeddingOptions;
    private readonly ILogger<AssistantUseCase> _logger;

    public AssistantUseCase(
        IAiClient aiClient,
        PromptComposer promptComposer,
        IConversationRepository conversationRepository,
        IVectorStore vectorStore,
        IEmbeddingService embeddingService,
        IOptions<RagOptions> ragOptions,
        IOptions<EmbeddingOptions> embeddingOptions,
        ILogger<AssistantUseCase> logger)
    {
        _aiClient = aiClient;
        _promptComposer = promptComposer;
        _conversationRepository = conversationRepository;
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _ragOptions = ragOptions;
        _embeddingOptions = embeddingOptions;
        _logger = logger;
    }

    public async Task<string> Execute(
        string conversationId,
        string userId,
        string userInput,
        JudgeBotConfiguration config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        var rag = _ragOptions.Value;
        var conversation = await _conversationRepository.GetByIdAsync(conversationId).ConfigureAwait(false);

        var ordered = conversation?.Messages.OrderBy(m => m.CreatedAt).ToList() ?? [];
        var windowList = ordered.Count == 0
            ? (IReadOnlyList<Message>)Array.Empty<Message>()
            : ordered.TakeLast(Math.Max(1, rag.MaxSlidingWindowMessages)).ToList();

        var historyMessages = windowList.Count > 0 && windowList[^1].Speaker == MessageSpeaker.User
            ? windowList.SkipLast(1).ToList()
            : windowList;

        var windowIds = new HashSet<Guid>(windowList.Select(m => m.Id));
        var historyLines = FormatHistoryLines(historyMessages, rag.MaxCharsPerMessage);
        var retrievedLines = await TryRetrieveContextLinesAsync(
                conversationId,
                userInput,
                windowIds,
                rag,
                cancellationToken)
            .ConfigureAwait(false);

        var prompt = _promptComposer.BuildHybridChatPrompt(
            config.Persona,
            retrievedLines,
            historyLines,
            userInput ?? string.Empty);

        _logger.LogInformation(
            "AssistantUseCase | personaId={PersonaId} userId={UserId} conversationId={ConversationId} promptChars={PromptChars} retrievalLines={RetrievalCount} historyLines={HistoryCount}",
            config.Persona.Id,
            userId,
            conversationId,
            prompt.Length,
            retrievedLines.Count,
            historyLines.Count);

        return await _aiClient.ChatAsync(userId, prompt).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<string>> TryRetrieveContextLinesAsync(
        string conversationId,
        string? userInput,
        HashSet<Guid> slidingWindowMessageIds,
        RagOptions rag,
        CancellationToken cancellationToken)
    {
        if (!rag.Enabled || string.IsNullOrWhiteSpace(userInput))
            return Array.Empty<string>();

        var embOpts = _embeddingOptions.Value;
        if (string.Equals(embOpts.Provider, "Placeholder", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(embOpts.Endpoint))
        {
            return Array.Empty<string>();
        }

        var queryText = Truncate(userInput, rag.MaxCharsPerMessage);
        float[] vector;
        try
        {
            vector = await _embeddingService.GenerateEmbeddingAsync(queryText, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AssistantUseCase: embedding request failed; continuing without retrieval.");
            return Array.Empty<string>();
        }

        if (vector.Length != 1536 || IsAllZeros(vector))
            return Array.Empty<string>();

        IReadOnlyList<VectorSearchResult> hits;
        try
        {
            hits = await _vectorStore
                .SearchAsync(vector, rag.MaxVectorResults, conversationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AssistantUseCase: vector search failed; continuing without retrieval.");
            return Array.Empty<string>();
        }

        var lines = new List<string>();
        var i = 0;
        foreach (var hit in hits)
        {
            if (slidingWindowMessageIds.Contains(hit.MessageId))
                continue;

            var text = Truncate(hit.Content, rag.MaxCharsPerMessage);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            i++;
            lines.Add($"[{i}] (similarity {hit.SimilarityScore:F2}) {text}");
        }

        return lines;
    }

    private static IReadOnlyList<string> FormatHistoryLines(IReadOnlyList<Message> messages, int maxChars)
    {
        var lines = new List<string>(messages.Count);
        foreach (var m in messages)
        {
            var role = m.Speaker == MessageSpeaker.Assistant ? "Assistant" : "User";
            var text = Truncate(m.Text, maxChars);
            lines.Add($"{role}: {text}");
        }

        return lines;
    }

    private static string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value) || maxChars <= 0)
            return string.Empty;
        return value.Length <= maxChars ? value : value[..maxChars];
    }

    private static bool IsAllZeros(float[] vector)
    {
        foreach (var x in vector)
        {
            if (x != 0f)
                return false;
        }

        return true;
    }
}
