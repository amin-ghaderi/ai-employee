namespace AiEmployee.Application;

/// <summary>
/// Thrown when the external AI/HTTP classification or judge call fails in a recoverable way
/// (bad HTTP status, timeout, invalid or empty JSON). Webhook may return 200 after user notification to limit provider retries.
/// </summary>
public sealed class AiServiceException : Exception
{
    public AiServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
