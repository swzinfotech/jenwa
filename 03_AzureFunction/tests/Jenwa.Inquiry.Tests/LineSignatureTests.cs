using System.Security.Cryptography;
using System.Text;
using Jenwa.Inquiry.Services;
using Xunit;

namespace Jenwa.Inquiry.Tests;

public class LineSignatureTests
{
    const string Secret = "cc3245dbe384e70ce12b9b873f5c8cec_test_only";

    static string Sign(string body, string secret)
        => Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));

    [Fact]
    public void Accepts_a_signature_produced_with_the_channel_secret()
    {
        const string body = """{"destination":"Uabc","events":[]}""";
        Assert.True(LineSignature.Verify(Encoding.UTF8.GetBytes(body), Secret, Sign(body, Secret)));
    }

    [Fact]
    public void Rejects_a_signature_made_with_another_secret()
    {
        const string body = """{"events":[]}""";
        Assert.False(LineSignature.Verify(Encoding.UTF8.GetBytes(body), Secret, Sign(body, "wrong-secret")));
    }

    [Fact]
    public void Rejects_a_body_that_was_modified_after_signing()
    {
        var signature = Sign("""{"events":[{"type":"message"}]}""", Secret);
        Assert.False(LineSignature.Verify(Encoding.UTF8.GetBytes("""{"events":[{"type":"join"}]}"""), Secret, signature));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!!")]
    [InlineData("c2hvcnQ=")] // valid base64, wrong length
    public void Rejects_a_missing_or_malformed_header(string? header)
        => Assert.False(LineSignature.Verify(Encoding.UTF8.GetBytes("{}"), Secret, header));

    [Fact]
    public void Rejects_everything_when_the_secret_is_not_configured()
    {
        const string body = "{}";
        Assert.False(LineSignature.Verify(Encoding.UTF8.GetBytes(body), "", Sign(body, "")));
    }

    [Fact]
    public void Accepts_an_empty_body_the_console_verify_button_sends()
    {
        Assert.True(LineSignature.Verify(Array.Empty<byte>(), Secret, Sign("", Secret)));
    }
}
