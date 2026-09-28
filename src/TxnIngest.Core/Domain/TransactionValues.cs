namespace TxnIngest.Core.Domain;

/// <summary>
/// Typed but unchecked values that an adapter reads from a partner row.
/// A value the adapter could not read is null.
/// </summary>
public sealed class TransactionValues
{
    public SourceSystem? Source { get; init; }
    public string? ExternalId { get; init; }
    public string? MerchantId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
    public decimal? Amount { get; init; }
    public Currency? Currency { get; init; }
    public TransactionType? Type { get; init; }
    public Dictionary<string, string> UnmappedValues { get; } = new();
}
