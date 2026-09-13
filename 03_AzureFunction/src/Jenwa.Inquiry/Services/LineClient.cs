using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Jenwa.Inquiry.Services;

/// <summary>Thin wrapper over the LINE Messaging API (push and reply).</summary>
public sealed class LineClient(HttpClient http, AppConfig config, ILogger<LineClient> logger)
{
    /// <summary>
    /// Pushes one text message to every configured recipient.
    /// Returns the number of recipients that accepted it; 0 means nobody was notified.
    /// </summary>
    public async Task<int> PushAsync(string text, string retryKey, CancellationToken cancellationToken)
    {
        if (config.ToIds.Count == 0)
        {
            logger.LogError("LINE_TO_IDS is empty — nothing was pushed. Set it to the staff group id.");
            return 0;
        }

        var delivered = 0;
        foreach (var to in config.ToIds)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/push")
            {
                Content = JsonContent.Create(new
                {
                    to,
                    messages = new[] { new { type = "text", text } },
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ChannelAccessToken);
            // Makes a retried push idempotent, so a transient failure cannot double-notify the group.
            request.Headers.TryAddWithoutValidation("X-Line-Retry-Key", DeterministicRetryKey(retryKey, to));

            try
            {
                using var response = await http.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    delivered++;
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("LINE push to {To} failed: {Status} {Body}", Mask(to), (int)response.StatusCode, body);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "LINE push to {To} threw.", Mask(to));
            }
        }
        return delivered;
    }

    /// <summary>Best-effort reply to a webhook event; failures are logged and swallowed.</summary>
    public async Task ReplyAsync(string replyToken, string text, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/reply")
        {
            Content = JsonContent.Create(new
            {
                replyToken,
                messages = new[] { new { type = "text", text } },
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ChannelAccessToken);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("LINE reply failed: {Status} {Body}", (int)response.StatusCode, body);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "LINE reply threw.");
        }
    }

    /// <summary>X-Line-Retry-Key must be a UUID, and the same payload must always map to the same one.</summary>
    static string DeterministicRetryKey(string seed, string to)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{seed}|{to}"));
        return new Guid(hash.AsSpan(0, 16)).ToString();
    }

    /// <summary>Recipient ids are personal data; keep only enough to tell recipients apart in logs.</summary>
    static string Mask(string id) => id.Length <= 8 ? "***" : $"{id[..5]}…{id[^3..]}";
}
