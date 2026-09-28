using System.Text.Json;
using System.Text.Json.Serialization;
using TxnIngest.Core.Ingestion;
using TxnIngest.Core.Sources.Harbour;

namespace TxnIngest.Core.Responses;

public static class ResponseFileFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ResponseFile Format(FileReceipt receipt, IReadOnlyList<RowResult> rows)
    {
        // Only Harbour files exist today.
        var adapter = new HarbourAdapter();

        var records = rows
            .OrderBy(r => r.RowNumber)
            .Select(r => new ResponseRecord(
                r.RowNumber,
                r.ExternalId,
                r.Accepted ? "Accepted" : "Rejected",
                r.Errors
                    .Select(e => new ResponseError(adapter.ColumnFor(e.Field), e.Code.ToString(), e.Message))
                    .ToList()))
            .ToList();

        return new ResponseFile(
            receipt.FileName,
            receipt.Token,
            TruncateToSeconds(receipt.ProcessedAtUtc ?? receipt.ReceivedAtUtc),
            receipt.State,
            receipt.RowsReceived,
            receipt.RowsAccepted,
            receipt.RowsRejected,
            receipt.FailureReason,
            records);
    }

    private static DateTime TruncateToSeconds(DateTime time)
        => new(time.Ticks - time.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

    public static string ToJson(ResponseFile response) => JsonSerializer.Serialize(response, JsonOptions);
}
