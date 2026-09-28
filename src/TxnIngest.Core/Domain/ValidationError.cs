namespace TxnIngest.Core.Domain;

public enum ValidationCode
{
    Required,
    Invalid,
    TooLong,
    OutOfRange,
    Duplicate
}

/// <param name="Field">One of the <see cref="TransactionField"/> constants.</param>
public sealed record ValidationError(string Field, ValidationCode Code, string Message);
