using System.Threading;

namespace AiEmployee.Application.Admin;

public static class AiDebugContext
{
    private static readonly AsyncLocal<string?> LastRawResponse = new();

    public static void SetLastRawResponse(string? rawResponse)
    {
        LastRawResponse.Value = rawResponse;
    }

    public static string? GetLastRawResponse()
    {
        return LastRawResponse.Value;
    }

    public static void Clear()
    {
        LastRawResponse.Value = null;
    }
}
