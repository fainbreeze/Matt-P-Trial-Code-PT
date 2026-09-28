using TxnIngest.Core.Domain;

namespace TxnIngest.Core.Ingestion;

/// <summary>What happened to one data row of a file.</summary>
public sealed class RowResult
{
    public int Id { get; set; }
    public Guid ReceiptToken { get; set; }

    /// <summary>1-based, counting data rows only.</summary>
    public int RowNumber { get; set; }

    /// <summary>The identifier as read from the file. Can be null.</summary>
    public string? ExternalId { get; set; }

    public bool Accepted { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}
