namespace AiEmployee.Application.Options;

/// <summary>Runtime limits and tuning for gateway (omnichannel handoff) dispatch.</summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>Maximum characters forwarded to the gateway destination (longer content is truncated).</summary>
    public int MaxForwardedMessageChars { get; set; } = 16000;
}
