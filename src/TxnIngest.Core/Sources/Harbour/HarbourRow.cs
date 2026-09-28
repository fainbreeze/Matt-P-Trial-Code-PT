namespace TxnIngest.Core.Sources.Harbour;

/// <summary>One data row of a Harbour Bank file, exactly as read. Every value is text.</summary>
public sealed class HarbourRow
{
    public string? TransactionId { get; set; }
    public string? MerchantId { get; set; }
    public string? TransactionDatetime { get; set; }
    public string? Amount { get; set; }
    public string? Currency { get; set; }
    public string? TransactionType { get; set; }
    public string? AuthCode { get; set; }
}
