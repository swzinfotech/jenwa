using Jenwa.Inquiry;
using Jenwa.Inquiry.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jenwa.Inquiry.Tests;

public class CorsTests
{
    static HttpRequest Request(string? origin = null, string? forwardedFor = null, string? remoteIp = null)
    {
        var context = new DefaultHttpContext();
        if (origin is not null) context.Request.Headers.Origin = origin;
        if (forwardedFor is not null) context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        if (remoteIp is not null) context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        return context.Request;
    }

    static AppConfig Config(params string[] origins) => new() { AllowedOrigins = origins };

    [Fact]
    public void Echoes_an_allowed_origin()
        => Assert.Equal("https://easygo.example", Cors.Resolve(Request("https://easygo.example"), Config("https://easygo.example")));

    [Fact]
    public void Refuses_an_origin_outside_the_allow_list()
        => Assert.Null(Cors.Resolve(Request("https://evil.example"), Config("https://easygo.example")));

    [Fact]
    public void Allows_a_request_with_no_origin_header()
        => Assert.Equal("", Cors.Resolve(Request(forwardedFor: "1.2.3.4:5678"), Config("https://easygo.example")));

    [Fact]
    public void Allows_any_origin_when_the_allow_list_is_a_wildcard()
        => Assert.Equal("https://anything.example", Cors.Resolve(Request("https://anything.example"), Config("*")));

    [Fact]
    public void Allows_any_origin_when_the_allow_list_is_unset()
        => Assert.Equal("https://anything.example", Cors.Resolve(Request("https://anything.example"), Config()));

    [Theory]
    [InlineData("203.0.113.7:41234", "203.0.113.7")]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("203.0.113.7:41234, 70.41.3.18:1234", "203.0.113.7")]
    [InlineData("[2001:db8::1]:41234", "2001:db8::1")]
    [InlineData("2001:db8::1", "2001:db8::1")]
    public void Reads_the_client_address_azure_forwards(string header, string expected)
        => Assert.Equal(expected, Cors.ClientIp(Request(forwardedFor: header)));

    [Fact]
    public void Falls_back_to_the_connection_address()
        => Assert.Equal("198.51.100.9", Cors.ClientIp(Request(remoteIp: "198.51.100.9")));

    [Fact]
    public void Reports_unknown_rather_than_throwing_when_there_is_no_address()
        => Assert.Equal("unknown", Cors.ClientIp(Request()));
}
