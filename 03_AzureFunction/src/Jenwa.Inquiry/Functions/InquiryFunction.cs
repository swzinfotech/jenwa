using System.Text.Json;
using Jenwa.Inquiry.Models;
using Jenwa.Inquiry.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jenwa.Inquiry.Functions;

/// <summary>POST /api/inquiry — the site form's endpoint. Validates, throttles, then pushes to LINE.</summary>
public sealed class InquiryFunction(
    AppConfig config,
    ThrottleStore throttle,
    InquiryStore inquiries,
    LineClient line,
    ILogger<InquiryFunction> logger)
{
    [Function("Inquiry")]
    public async Task<IActionResult> Run(
        // No "options": the Functions host answers the CORS preflight from the platform CORS
        // setting without ever invoking the function.
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inquiry")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (Cors.Resolve(request, config) is null)
        {
            logger.LogWarning("Rejected inquiry from disallowed origin {Origin}.", request.Headers.Origin.ToString());
            return Fail(StatusCodes.Status403Forbidden, "來源網域未被允許。");
        }

        // ReadFromJsonAsync throws for a missing or non-JSON Content-Type; checking first keeps a
        // malformed request a 400 instead of an unhandled 500.
        if (!request.HasJsonContentType())
            return Fail(StatusCodes.Status415UnsupportedMediaType, "請求內容需為 JSON。");

        InquiryRequest? payload;
        try
        {
            payload = await request.ReadFromJsonAsync<InquiryRequest>(cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogInformation("Rejected an unreadable inquiry body: {Message}", ex.Message);
            return Fail(StatusCodes.Status400BadRequest, "請求內容無法解析。");
        }

        var validation = InquiryValidator.Validate(payload);
        if (validation.Outcome == ValidationOutcome.Invalid)
            return Fail(StatusCodes.Status400BadRequest, validation.Error!);

        if (validation.Outcome == ValidationOutcome.SilentDrop)
        {
            // Answer exactly like a success so an automated submitter gets no signal.
            logger.LogInformation("Dropped a submission silently: {Reason}.", validation.Reason);
            return new OkObjectResult(new { ok = true });
        }

        var now = DateTimeOffset.UtcNow;
        var decision = await throttle.CheckAsync(
            Cors.ClientIp(request), InquiryValidator.NormalisePhone(payload!.Phone), now, cancellationToken);
        if (decision != ThrottleDecision.Allowed)
        {
            var message = decision == ThrottleDecision.TooSoon
                ? "剛才已收到您的諮詢，請稍候再送出。"
                : "今日送出次數已達上限，請改用電話聯繫我們。";
            return Fail(StatusCodes.Status429TooManyRequests, message);
        }

        var id = InquiryStore.NewId(now);
        var withinCap = await throttle.TryReservePushAsync(config.MonthlyPushCap, now, cancellationToken);

        var delivered = 0;
        if (withinCap)
        {
            var text = InquiryValidator.BuildMessage(payload, now);
            delivered = await line.PushAsync(text, id, cancellationToken);
        }

        // Store after pushing so the record says whether the group was actually notified.
        try
        {
            await inquiries.SaveAsync(id, payload, now, delivered > 0, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not store inquiry {Id}.", id);
        }

        if (!withinCap || delivered == 0)
        {
            // The lead is safe in the table either way, so the visitor still sees a success.
            logger.LogError("Inquiry {Id} was not delivered to LINE (withinCap={WithinCap}, delivered={Delivered}).",
                id, withinCap, delivered);
            return new OkObjectResult(new { ok = true, queued = true });
        }

        logger.LogInformation("Inquiry {Id} pushed to {Count} LINE recipient(s).", id, delivered);
        return new OkObjectResult(new { ok = true });
    }

    static ObjectResult Fail(int status, string error) => new(new { ok = false, error }) { StatusCode = status };
}
