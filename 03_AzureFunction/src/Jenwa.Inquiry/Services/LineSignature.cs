using System.Security.Cryptography;
using System.Text;

namespace Jenwa.Inquiry.Services;

/// <summary>
/// Validates the X-Line-Signature header: base64(HMAC-SHA256(channel secret, raw request body)).
/// The signature covers the raw bytes, so the body must never be re-serialised before checking.
/// </summary>
public static class LineSignature
{
    public static bool Verify(ReadOnlySpan<byte> body, string channelSecret, string? signatureHeader)
    {
        if (string.IsNullOrEmpty(channelSecret) || string.IsNullOrWhiteSpace(signatureHeader)) return false;

        Span<byte> expected = stackalloc byte[32];
        if (!HMACSHA256.TryHashData(Encoding.UTF8.GetBytes(channelSecret), body, expected, out _)) return false;

        Span<byte> actual = stackalloc byte[32];
        if (!Convert.TryFromBase64String(signatureHeader, actual, out var written) || written != expected.Length) return false;

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
