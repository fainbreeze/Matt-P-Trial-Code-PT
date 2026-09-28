using TxnIngest.Core.Domain;

namespace TxnIngest.Core.Ingestion;

/// <summary>The record of one file we were given, whatever happened to it.</summary>
public sealed class FileReceipt
{
    public Guid Token { get; set; }
    public SourceSystem Source { get; set; }
    public string FileName { get; set; } = "";
    public string Hash { get; set; } = "";
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public FileState State { get; set; }
    public int RowsReceived { get; set; }
    public int RowsAccepted { get; set; }
    public int RowsRejected { get; set; }

    /// <summary>Why the whole file failed. Null for every other state.</summary>
    public string? FailureReason { get; set; }
}
