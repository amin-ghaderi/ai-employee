using System.Net.Http.Json;
using System.Text.Json;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.AI;

/// <summary>
/// HTTP-capable embedding client with a safe placeholder when <see cref="EmbeddingOptions.Provider"/> is <c>Placeholder</c> or endpoint is unset.
/// </summary>
public sealed class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<EmbeddingOptions> _options;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        HttpClient httpClient,
        IOptions<EmbeddingOptions> options,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var opt = _options.Value;
        var dims = opt.Dimensions <= 0 ? 1536 : opt.Dimensions;

        if (string.IsNullOrWhiteSpace(text))
            return ZeroVector(dims);

        if (string.Equals(opt.Provider, "Placeholder", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(opt.Endpoint))
        {
            _logger.LogDebug("EmbeddingService: returning zero vector (placeholder), dimensions={Dimensions}", dims);
            return ZeroVector(dims);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, opt.Endpoint.TrimEnd('/'));
            request.Content = JsonContent.Create(
                new { model = opt.Model, input = text },
                options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (!string.IsNullOrWhiteSpace(opt.ApiKey))
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {opt.ApiKey.Trim()}");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            // OpenAI-style: { "data": [ { "embedding": [ ... ] } ] }
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Array &&
                data.GetArrayLength() > 0)
            {
                var first = data[0];
                if (first.TryGetProperty("embedding", out var emb) && emb.ValueKind == JsonValueKind.Array)
                {
                    var arr = new float[emb.GetArrayLength()];
                    var i = 0;
                    foreach (var el in emb.EnumerateArray())
                    {
                        if (i >= arr.Length)
                            break;
                        arr[i++] = el.GetSingle();
                    }

                    if (arr.Length == dims)
                        return arr;
                    if (arr.Length > dims)
                        return arr.AsSpan(0, dims).ToArray();
                    if (arr.Length > 0)
                    {
                        var padded = ZeroVector(dims);
                        arr.AsSpan().CopyTo(padded);
                        return padded;
                    }
                }
            }

            _logger.LogWarning("EmbeddingService: unexpected response shape; returning zero vector.");
            return ZeroVector(dims);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EmbeddingService: HTTP embedding failed; returning zero vector.");
            return ZeroVector(dims);
        }
    }

    private static float[] ZeroVector(int dimensions)
    {
        var v = new float[dimensions];
        Array.Clear(v);
        return v;
    }
}
