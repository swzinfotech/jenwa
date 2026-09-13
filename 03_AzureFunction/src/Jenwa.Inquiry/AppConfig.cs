using Microsoft.Extensions.Configuration;

namespace Jenwa.Inquiry;

/// <summary>App settings; every secret comes from Azure App Settings, never from the repo.</summary>
public sealed class AppConfig
{
    public string ChannelAccessToken { get; init; } = "";
    public string ChannelSecret { get; init; } = "";
    public IReadOnlyList<string> ToIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AllowedOrigins { get; init; } = Array.Empty<string>();
    public int MonthlyPushCap { get; init; } = 150;
    public string StorageConnection { get; init; } = "";

    public static AppConfig FromConfiguration(IConfiguration configuration) => new()
    {
        ChannelAccessToken = configuration["LINE_CHANNEL_ACCESS_TOKEN"] ?? "",
        ChannelSecret = configuration["LINE_CHANNEL_SECRET"] ?? "",
        ToIds = Split(configuration["LINE_TO_IDS"]),
        AllowedOrigins = Split(configuration["ALLOWED_ORIGINS"]),
        MonthlyPushCap = int.TryParse(configuration["MONTHLY_PUSH_CAP"], out var cap) ? cap : 150,
        StorageConnection = configuration["AzureWebJobsStorage"] ?? "",
    };

    static string[] Split(string? value) => string.IsNullOrWhiteSpace(value)
        ? Array.Empty<string>()
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
