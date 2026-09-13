using Azure.Data.Tables;

namespace Jenwa.Inquiry.Services;

/// <summary>
/// Resolves a table once per app instance. CreateIfNotExists is cheap but not free, so the
/// Lazy keeps it to a single call instead of one per request.
/// </summary>
public abstract class TableStoreBase
{
    readonly Lazy<Task<TableClient>> table;

    protected TableStoreBase(TableServiceClient service, string tableName)
        => table = new(async () =>
        {
            var client = service.GetTableClient(tableName);
            await client.CreateIfNotExistsAsync();
            return client;
        });

    protected Task<TableClient> GetTableAsync() => table.Value;

    /// <summary>
    /// Hashes a value into a safe row key. IP addresses and phone numbers are personal data and
    /// only need to be comparable, never readable, so the throttle tables store digests.
    /// </summary>
    protected static string HashKey(string value)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    /// <summary>Buckets are keyed on Taipei local time so "per day" matches the office's day.</summary>
    protected static DateTimeOffset ToTaipei(DateTimeOffset instant) => instant.ToOffset(TimeSpan.FromHours(8));
}
