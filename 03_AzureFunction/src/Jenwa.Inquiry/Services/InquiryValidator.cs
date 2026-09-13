using System.Text;
using System.Text.RegularExpressions;
using Jenwa.Inquiry.Models;

namespace Jenwa.Inquiry.Services;

public enum ValidationOutcome
{
    /// <summary>Accept and push.</summary>
    Ok,
    /// <summary>Reject with 400 and tell the visitor what is wrong.</summary>
    Invalid,
    /// <summary>Looks automated. Answer 200 but do nothing, so the bot learns nothing.</summary>
    SilentDrop,
}

public readonly record struct ValidationResult(ValidationOutcome Outcome, string? Error, string? Reason);

/// <summary>
/// Field rules mirror the client-side constraints in 02_website/build.mjs so a visitor who
/// passes the browser validation is never rejected here, and pure enough to unit test.
/// </summary>
public static partial class InquiryValidator
{
    /// <summary>A real person needs at least this long to fill the form in.</summary>
    public const double MinimumElapsedMs = 2000;

    [GeneratedRegex(@"^[+0-9() .\-]{6,30}$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex EmailPattern();

    public static ValidationResult Validate(InquiryRequest? request)
    {
        if (request is null) return new(ValidationOutcome.Invalid, "請求內容無法解析。", "unparsable body");

        if (!string.IsNullOrWhiteSpace(request.CompanyUrl))
            return new(ValidationOutcome.SilentDrop, null, "honeypot filled");

        // Elapsed is only trusted as a bot signal: 0 means the field was stripped, which is itself suspicious,
        // but we allow it so that an older cached app.js keeps working.
        if (request.Elapsed > 0 && request.Elapsed < MinimumElapsedMs)
            return new(ValidationOutcome.SilentDrop, null, $"submitted after {request.Elapsed:F0}ms");

        if (IsBlank(request.Name) || Trim(request.Name).Length > 80)
            return new(ValidationOutcome.Invalid, "請填寫姓名（80 字以內）。", "name");

        var phone = Trim(request.Phone);
        if (!PhonePattern().IsMatch(phone))
            return new(ValidationOutcome.Invalid, "請填寫 6 至 30 個字元的聯絡電話。", "phone");

        if (IsBlank(request.Location) || Trim(request.Location).Length > 120)
            return new(ValidationOutcome.Invalid, "請填寫基地縣市／地區（120 字以內）。", "location");

        var email = Trim(request.Email);
        if (email.Length > 0 && (email.Length > 120 || !EmailPattern().IsMatch(email)))
            return new(ValidationOutcome.Invalid, "電子信箱格式不正確。", "email");

        if (Trim(request.Purpose).Length > 40)
            return new(ValidationOutcome.Invalid, "規劃用途格式不正確。", "purpose");

        if (Trim(request.Message).Length > 3000)
            return new(ValidationOutcome.Invalid, "需求說明請控制在 3000 字以內。", "message");

        return new(ValidationOutcome.Ok, null, null);
    }

    /// <summary>LINE text messages are capped at 5000 characters; the validated fields total well under that.</summary>
    public static string BuildMessage(InquiryRequest request, DateTimeOffset receivedUtc)
    {
        var taipei = receivedUtc.ToOffset(TimeSpan.FromHours(8)); // Taiwan has no DST, so a fixed offset is safe.
        return new StringBuilder()
            .AppendLine("【易立構】新場勘諮詢")
            .AppendLine($"姓名：{Present(request.Name)}")
            .AppendLine($"電話：{Present(request.Phone)}")
            .AppendLine($"信箱：{Present(request.Email)}")
            .AppendLine($"基地：{Present(request.Location)}")
            .AppendLine($"用途：{Present(request.Purpose)}")
            .AppendLine($"需求：{Present(request.Message)}")
            .Append($"時間：{taipei:yyyy/MM/dd HH:mm}（台北）")
            .ToString();
    }

    /// <summary>Digits only, so "0912-345-678" and "0912 345 678" throttle as the same person.</summary>
    public static string NormalisePhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";
        var digits = new StringBuilder(phone.Length);
        foreach (var c in phone) if (char.IsAsciiDigit(c)) digits.Append(c);
        return digits.ToString();
    }

    static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
    static string Trim(string? value) => value?.Trim() ?? "";
    static string Present(string? value) => IsBlank(value) ? "—" : Trim(value);
}
