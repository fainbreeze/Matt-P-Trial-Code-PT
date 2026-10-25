namespace TxnIngest.Core.Domain;

/// <summary>The direction of money is carried here. <see cref="Transaction.Amount"/> is always positive.</summary>
public enum TransactionType
{
    Purchase = 1,
    Refund = 2,
    Void = 3
}
