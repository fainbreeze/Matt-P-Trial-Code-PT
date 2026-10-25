namespace TxnIngest.Core.Domain;

/// <summary>The partner a transaction file came from. Stored as text in the database.</summary>
public enum SourceSystem
{
    Harbour = 1,
    Maple = 2
}
