namespace TxnIngest.Core.Domain;

/// <summary>Our names for the fields of a <see cref="Transaction"/>, used to report validation errors.</summary>
public static class TransactionField
{
    public const string Source = nameof(Transaction.Source);
    public const string ExternalId = nameof(Transaction.ExternalId);
    public const string MerchantId = nameof(Transaction.MerchantId);
    public const string OccurredAtUtc = nameof(Transaction.OccurredAtUtc);
    public const string Amount = nameof(Transaction.Amount);
    public const string Currency = nameof(Transaction.Currency);
    public const string Type = nameof(Transaction.Type);
}
