using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Jenwa.Inquiry.Services;

public enum ThrottleDecision { Allowed, TooSoon, DailyLimit }

/// <summary>
/// Abuse control for a public endpoint. The LINE plan only allows 200 pushes a month, so the
/// point is not just to stop spam but to make sure a flood cannot exhaust the quota.
/// </summary>
public sealed class ThrottleStore(TableServiceClient service, ILogger<ThrottleStore> logger)
    : TableStoreBase(service, "throttle")
{
    public static readonly TimeSpan IpMinimumInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Deliberately loose: mobile carriers here put many subscribers behind one public address,
    /// so a tight per-IP daily cap turns unrelated customers away. The 60-second interval is what
    /// actually stops a flood, and the monthly push cap is the backstop.
    /// </summary>
    public const int IpDailyLimit = 20;
    public static readonly TimeSpan PhoneMinimumInterval = TimeSpan.FromMinutes(10);

    public async Task<ThrottleDecision> CheckAsync(string clientIp, string phoneDigits, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var ip = await TryConsumeAsync("ip", clientIp, IpMinimumInterval, IpDailyLimit, now, cancellationToken);
        if (ip != ThrottleDecision.Allowed) return ip;

        if (phoneDigits.Length > 0)
        {
            var phone = await TryConsumeAsync("phone", phoneDigits, PhoneMinimumInterval, 0, now, cancellationToken);
            if (phone != ThrottleDecision.Allowed) return phone;
        }
        return ThrottleDecision.Allowed;
    }

    async Task<ThrottleDecision> TryConsumeAsync(
        string partition, string key, TimeSpan minimumInterval, int dailyLimit, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var table = await GetTableAsync();
        var rowKey = HashKey(key);
        var dayKey = ToTaipei(now).ToString("yyyyMMdd");

        TableEntity? existing = null;
        try
        {
            existing = (await table.GetEntityAsync<TableEntity>(partition, rowKey, cancellationToken: cancellationToken)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // First time we have seen this key.
        }

        var entity = new TableEntity(partition, rowKey);
        if (existing is not null)
        {
            var lastAt = existing.GetDateTimeOffset("LastAt") ?? DateTimeOffset.MinValue;
            if (now - lastAt < minimumInterval) return ThrottleDecision.TooSoon;

            var sameDay = existing.GetString("DayKey") == dayKey;
            var count = sameDay ? existing.GetInt32("DayCount") ?? 0 : 0;
            if (dailyLimit > 0 && count >= dailyLimit) return ThrottleDecision.DailyLimit;

            entity["DayCount"] = count + 1;
        }
        else
        {
            entity["DayCount"] = 1;
        }

        entity["LastAt"] = now;
        entity["DayKey"] = dayKey;
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        return ThrottleDecision.Allowed;
    }

    /// <summary>
    /// Counts one push against the month's cap. Returns false when the cap is reached, in which
    /// case the caller still stores the inquiry — it just does not reach LINE.
    /// </summary>
    public async Task<bool> TryReservePushAsync(int monthlyCap, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (monthlyCap <= 0) return true;

        var table = await GetTableAsync();
        var rowKey = ToTaipei(now).ToString("yyyyMM");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var response = await table.GetEntityIfExistsAsync<TableEntity>("quota", rowKey, cancellationToken: cancellationToken);
                if (!response.HasValue)
                {
                    var created = new TableEntity("quota", rowKey) { ["Count"] = 1 };
                    await table.AddEntityAsync(created, cancellationToken);
                    return true;
                }

                var entity = response.Value!;
                var count = entity.GetInt32("Count") ?? 0;
                if (count >= monthlyCap)
                {
                    logger.LogWarning("Monthly LINE push cap {Cap} reached for {Month}; inquiry stored but not pushed.", monthlyCap, rowKey);
                    return false;
                }

                entity["Count"] = count + 1;
                await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status is 409 or 412)
            {
                // Another instance updated the counter first; re-read and try again.
            }
        }

        logger.LogWarning("Could not reserve push quota after retries; allowing the push.");
        return true;
    }
}
