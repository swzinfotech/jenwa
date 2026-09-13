using Azure.Data.Tables;

namespace Jenwa.Inquiry.Services;

public sealed record LineSource(string SourceType, string Id, string LastEventType, DateTimeOffset LastSeen, int EventCount);

/// <summary>
/// Records who talks to the bot. This is how LINE_TO_IDS gets its value: the Push API needs a
/// recipient id, and the webhook is the only place a group id is ever disclosed.
/// </summary>
public sealed class SourceStore(TableServiceClient service) : TableStoreBase(service, "linesources")
{
    public async Task RecordAsync(string sourceType, string id, string eventType, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var table = await GetTableAsync();
        var response = await table.GetEntityIfExistsAsync<TableEntity>(sourceType, id, cancellationToken: cancellationToken);

        var entity = new TableEntity(sourceType, id)
        {
            ["LastEventType"] = eventType,
            ["LastSeen"] = now,
            ["EventCount"] = (response.HasValue ? response.Value!.GetInt32("EventCount") ?? 0 : 0) + 1,
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<IReadOnlyList<LineSource>> ListAsync(CancellationToken cancellationToken)
    {
        var table = await GetTableAsync();
        var sources = new List<LineSource>();
        await foreach (var entity in table.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            sources.Add(new LineSource(
                entity.PartitionKey,
                entity.RowKey,
                entity.GetString("LastEventType") ?? "",
                entity.GetDateTimeOffset("LastSeen") ?? default,
                entity.GetInt32("EventCount") ?? 0));
        }
        return sources.OrderByDescending(source => source.LastSeen).ToList();
    }
}
