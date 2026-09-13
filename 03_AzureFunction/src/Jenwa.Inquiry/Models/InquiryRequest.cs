using System.Text.Json.Serialization;

namespace Jenwa.Inquiry.Models;

/// <summary>Payload posted by the site form (02_website, #inquiry-form).</summary>
public sealed class InquiryRequest
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("location")] public string? Location { get; set; }
    [JsonPropertyName("purpose")] public string? Purpose { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }

    /// <summary>Honeypot: hidden from people, filled in by naive bots.</summary>
    [JsonPropertyName("company_url")] public string? CompanyUrl { get; set; }

    /// <summary>Milliseconds the visitor spent on the form before submitting.</summary>
    [JsonPropertyName("elapsed")] public double Elapsed { get; set; }
}
