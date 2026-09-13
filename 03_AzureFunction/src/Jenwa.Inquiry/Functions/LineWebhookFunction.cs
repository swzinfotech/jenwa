using System.Text.Json;
using Jenwa.Inquiry.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jenwa.Inquiry.Functions;

/// <summary>
/// POST /api/line/webhook — the endpoint registered with the LINE channel.
/// Its job is to record which group or user is talking to the bot, so LINE_TO_IDS can be set.
/// </summary>
public sealed class LineWebhookFunction(
    AppConfig config,
    SourceStore sources,
    LineClient line,
    ILogger<LineWebhookFunction> logger)
{
    [Function("LineWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "line/webhook")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        // The signature covers the raw bytes, so buffer them before any parsing.
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);
        var body = buffer.ToArray();

        if (!LineSignature.Verify(body, config.ChannelSecret, request.Headers["X-Line-Signature"]))
        {
            logger.LogWarning("Rejected a webhook call with an invalid X-Line-Signature.");
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        try
        {
            await HandleEventsAsync(body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // LINE retries and eventually disables an endpoint that does not answer 200,
            // and a malformed event is not worth losing the webhook over.
            logger.LogError(ex, "Failed to handle a webhook payload.");
        }

        return new OkResult();
    }

    async Task HandleEventsAsync(byte[] body, CancellationToken cancellationToken)
    {
        if (body.Length == 0) return; // The "Verify" button in the LINE console sends an empty body.

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array) return;

        var now = DateTimeOffset.UtcNow;
        foreach (var element in events.EnumerateArray())
        {
            var eventType = element.TryGetProperty("type", out var type) ? type.GetString() ?? "" : "";
            if (!element.TryGetProperty("source", out var source)) continue;

            var (sourceType, id) = ReadSource(source);
            if (id is null) continue;

            await sources.RecordAsync(sourceType, id, eventType, now, cancellationToken);
            logger.LogInformation("Webhook {EventType} from {SourceType} recorded.", eventType, sourceType);

            // Only answer when someone is actually asking, so the bot stays quiet in the staff group.
            if (ShouldReply(eventType, element) &&
                element.TryGetProperty("replyToken", out var replyToken) &&
                replyToken.GetString() is { Length: > 0 } token)
            {
                var label = sourceType switch
                {
                    "group" => "本群組 ID",
                    "room" => "本聊天室 ID",
                    _ => "您的使用者 ID",
                };
                await line.ReplyAsync(token, $"{label}：\n{id}\n\n請將此 ID 設為 Azure Function 的 LINE_TO_IDS。", cancellationToken);
            }
        }
    }

    static (string SourceType, string? Id) ReadSource(JsonElement source)
    {
        if (source.TryGetProperty("groupId", out var group) && group.GetString() is { Length: > 0 } groupId)
            return ("group", groupId);
        if (source.TryGetProperty("roomId", out var room) && room.GetString() is { Length: > 0 } roomId)
            return ("room", roomId);
        if (source.TryGetProperty("userId", out var user) && user.GetString() is { Length: > 0 } userId)
            return ("user", userId);
        return ("unknown", null);
    }

    /// <summary>Reply when the bot is newly added, or when someone sends "id" / "/id" to ask for it.</summary>
    static bool ShouldReply(string eventType, JsonElement element)
    {
        if (eventType is "join" or "follow") return true;
        if (eventType != "message") return false;
        if (!element.TryGetProperty("message", out var message)) return false;
        if (!message.TryGetProperty("text", out var text)) return false;
        var value = text.GetString()?.Trim().TrimStart('/').ToLowerInvariant();
        return value is "id";
    }
}
