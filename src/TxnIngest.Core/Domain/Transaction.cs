namespace TxnIngest.Core.Domain;

/// <summary>A card transaction that passed validation.</summary>
public sealed class Transaction
{
    public const int MaxExternalIdLength = 50;
    public const int MaxMerchantIdLength = 20;

    private Transaction()
    {
    }

    public Guid Id { get; private set; }
    public SourceSystem Source { get; private set; }
    public string ExternalId { get; private set; } = "";
    public string MerchantId { get; private set; } = "";
    public DateTime OccurredAtUtc { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public TransactionType Type { get; private set; }
    public Guid ReceiptToken { get; internal set; }
    public Dictionary<string, string> UnmappedValues { get; private set; } = new();

    /// <summary>Checks every rule and returns all the errors, not only the first.</summary>
    public static Result<Transaction, IReadOnlyList<ValidationError>> Create(TransactionValues values, TimeProvider clock)
    {
        var errors = new List<ValidationError>();

        if (values.Source is null)
            errors.Add(Required(TransactionField.Source));

        var externalId = values.ExternalId?.Trim();
        if (string.IsNullOrEmpty(externalId))
            errors.Add(Required(TransactionField.ExternalId));
        else if (externalId.Length > MaxExternalIdLength)
            errors.Add(TooLong(TransactionField.ExternalId, MaxExternalIdLength));

        var merchantId = values.MerchantId?.Trim();
        if (string.IsNullOrEmpty(merchantId))
            errors.Add(Required(TransactionField.MerchantId));
        else if (merchantId.Length > MaxMerchantIdLength)
            errors.Add(TooLong(TransactionField.MerchantId, MaxMerchantIdLength));

        if (values.OccurredAtUtc is null)
            errors.Add(Required(TransactionField.OccurredAtUtc));
        else if (values.OccurredAtUtc.Value > clock.GetUtcNow().UtcDateTime)
            errors.Add(new ValidationError(TransactionField.OccurredAtUtc, ValidationCode.OutOfRange,
                "Transaction time must not be in the future."));

        if (values.Amount is null)
            errors.Add(Required(TransactionField.Amount));
        else if (values.Amount <= 0)
            errors.Add(new ValidationError(TransactionField.Amount, ValidationCode.OutOfRange,
                "Amount must be greater than 0."));
        else if (HasMoreThanTwoDecimals(values.Amount.Value))
            errors.Add(new ValidationError(TransactionField.Amount, ValidationCode.Invalid,
                "Amount must have at most 2 decimal places."));

        if (values.Currency is null)
            errors.Add(Required(TransactionField.Currency));

        if (values.Type is null)
            errors.Add(Required(TransactionField.Type));

        if (errors.Count > 0)
            return Result<Transaction, IReadOnlyList<ValidationError>>.Failure(errors);

        return Result<Transaction, IReadOnlyList<ValidationError>>.Success(new Transaction
        {
            Id = Guid.NewGuid(),
            Source = values.Source!.Value,
            ExternalId = externalId!,
            MerchantId = merchantId!,
            OccurredAtUtc = DateTime.SpecifyKind(values.OccurredAtUtc!.Value, DateTimeKind.Utc),
            Amount = values.Amount!.Value,
            Currency = values.Currency!.Value,
            Type = values.Type!.Value,
            UnmappedValues = new Dictionary<string, string>(values.UnmappedValues)
        });
    }

    private static bool HasMoreThanTwoDecimals(decimal amount)
    {
        var cents = amount * 100;
        return cents != decimal.Truncate(cents);
    }

    private static ValidationError Required(string field)
        => new(field, ValidationCode.Required, $"{field} is required.");

    private static ValidationError TooLong(string field, int max)
        => new(field, ValidationCode.TooLong, $"{field} must be at most {max} characters.");
}
