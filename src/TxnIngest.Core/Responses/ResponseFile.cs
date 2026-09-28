using TxnIngest.Core.Ingestion;

namespace TxnIngest.Core.Responses;

/// <summary>The report we send back to a partner about one file.</summary>
public sealed record ResponseFile(
    string SourceFileName,
    Guid Token,
    DateTime ProcessedAtUtc,
    FileState State,
    int RowsReceived,
    int RowsAccepted,
    int RowsRejected,
    string? FileError,
    IReadOnlyList<ResponseRecord> Records);

public sealed record ResponseRecord(
    int Row,
    string? TransactionId,
    string Status,
    IReadOnlyList<ResponseError> Errors);

public sealed record ResponseError(string Column, string Code, string Message);
