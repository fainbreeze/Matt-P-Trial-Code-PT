namespace TxnIngest.Core.Sources.Maple;

/// <summary>One data row of a Maple file, exactly as read. Every value is text.</summary>
public sealed class MapleRow
{
    public string? Ref { get; set; }
    public string? MerchantId { get; set; }
    public string? TransactionDate { get; set; }
    public string? TransactionTime { get; set; }
    public string? TransactionAmount { get; set; }
    public string? TransactionCode { get; set; }
    public string? TerminalId{ get; set; }
}
