using System.Globalization;
using TxnIngest.Core.Domain;

namespace TxnIngest.Core.Sources.Harbour;

/// <summary>
/// Converts between the Harbour columns and our own names, in both directions. No validation.
/// </summary>
public sealed class HarbourAdapter
{
    public TransactionValues ToValues(HarbourRow row)
    {
        var values = new TransactionValues
        {
            Source = SourceSystem.Harbour,
            ExternalId = ExternalIdFor(row),
            MerchantId = row.MerchantId,
            OccurredAtUtc = ReadDateTime(row.TransactionDatetime),
            Amount = ReadAmount(row.Amount),
            Currency = ReadCurrency(row.Currency),
            Type = ReadType(row.TransactionType)
        };

        // Our model has no home for these. They are kept, and never a reason to reject a row.
        AddUnmappedValue(values, "auth_code", row.AuthCode);

        return values;
    }

    /// <summary>
    /// Trimmed, because duplicate detection compares identifiers exactly and padding would otherwise let a
    /// resent transaction through as new.
    /// </summary>
    public string? ExternalIdFor(HarbourRow row) => row.TransactionId?.Trim();

    /// <summary>
    /// The Harbour column name for one of our field names, used in the report Harbour receives.
    /// </summary>
    /// <returns>The column name, or the field name when the problem is not about a column.</returns>
    public string ColumnFor(string transactionField)
        => Columns.TryGetValue(transactionField, out var column) ? column : transactionField;

    /// <summary>
    /// Returns UTC. A time with an offset is converted, and a time with none is taken as UTC. Text that will
    /// not read becomes null, and <see cref="Transaction.Create"/> then reports the value as missing.
    /// </summary>
    private static DateTime? ReadDateTime(string? text)
        => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
               ? parsed.UtcDateTime
               : null;

    private static decimal? ReadAmount(string? text)
        => decimal.TryParse(text,
                            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                            CultureInfo.InvariantCulture,
                            out var parsed)
               ? parsed
               : null;

    private static Currency? ReadCurrency(string? text) => text?.Trim().ToUpperInvariant() switch
    {
        "CAD" => Currency.CAD,
        "USD" => Currency.USD,
        _ => null
    };

    private static TransactionType? ReadType(string? text) => text?.Trim().ToUpperInvariant() switch
    {
        "SALE" => TransactionType.Purchase,
        "REFUND" => TransactionType.Refund,
        _ => null
    };

    private static void AddUnmappedValue(TransactionValues values, string column, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            values.UnmappedValues[column] = text;
    }

    private static readonly IDictionary<string, string> Columns = new Dictionary<string, string>
    {
        { TransactionField.ExternalId, "transaction_id" },
        { TransactionField.MerchantId, "merchant_id" },
        { TransactionField.OccurredAtUtc, "transaction_datetime" },
        { TransactionField.Amount, "amount" },
        { TransactionField.Currency, "currency" },
        { TransactionField.Type, "transaction_type" }
    };
}
