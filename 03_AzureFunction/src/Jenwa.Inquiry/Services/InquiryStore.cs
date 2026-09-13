using Azure.Data.Tables;
using Jenwa.Inquiry.Models;

namespace Jenwa.Inquiry.Services;

/// <summary>
/// Keeps every accepted inquiry. The LINE push is the notification, this is the record: if the
/// push fails or the monthly cap is hit, the lead is still recoverable from here.
/// </summary>
public sealed class InquiryStore(TableServiceClient service) : TableStoreBase(service, "inquiries")
{
    public async Task SaveAsync(string id, InquiryRequest request, DateTimeOffset now, bool pushed, CancellationToken cancellationToken)
    {
        var table = await GetTableAsync();
        var entity = new TableEntity(ToTaipei(now).ToString("yyyyMM"), id)
        {
            ["Name"] = request.Name?.Trim(),
            ["Phone"] = request.Phone?.Trim(),
            ["Email"] = request.Email?.Trim(),
            ["Location"] = request.Location?.Trim(),
            ["Purpose"] = request.Purpose?.Trim(),
            ["Message"] = request.Message?.Trim(),
            ["ReceivedAt"] = now,
            ["Pushed"] = pushed,
        };
        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    /// <summary>Row key that sorts newest first and is unique across concurrent submissions.</summary>
    public static string NewId(DateTimeOffset now)
        => $"{DateTimeOffset.MaxValue.Ticks - now.Ticks:D19}-{Guid.NewGuid():N}";
}
