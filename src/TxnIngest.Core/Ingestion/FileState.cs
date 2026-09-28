namespace TxnIngest.Core.Ingestion;

public enum FileState
{
    Received,
    Completed,
    Failed,
    DuplicateFile
}
