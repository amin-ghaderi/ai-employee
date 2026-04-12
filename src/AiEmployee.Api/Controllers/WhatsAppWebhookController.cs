using System.Security.Cryptography;
using System.Text;
using AiEmployee.Infrastructure.Integrations.WhatsApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiEmployee.Api.Controllers;

/// <summary>Meta WhatsApp Cloud API webhook verification (GET) and inbound events (POST). No admin key required.</summary>
[ApiController]
[Route("api/whatsapp/webhook")]
public sealed class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppSettings _settings;

    public WhatsAppWebhookController(IOptions<WhatsAppSettings> options)
    {
        _settings = options.Value;
    }

    /// <summary>Meta subscription verification (<c>hub.challenge</c> echo).</summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(challenge)
            && FixedTimeEqualsToken(_settings.VerifyToken, token))
        {
            return Content(challenge, "text/plain");
        }

        return Unauthorized();
    }

    /// <summary>Inbound webhook payload from Meta (acknowledge quickly).</summary>
    [HttpPost]
    public IActionResult Receive()
    {
        return Ok();
    }

    private static bool FixedTimeEqualsToken(string expected, string? provided)
    {
        if (string.IsNullOrEmpty(expected) || provided is null)
            return false;

        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(provided);
        if (a.Length != b.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
