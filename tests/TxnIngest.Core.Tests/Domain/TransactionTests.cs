using TxnIngest.Core.Domain;
using TxnIngest.Core.Tests.TestSupport;

namespace TxnIngest.Core.Tests.Domain;

[TestClass]
public class TransactionTests
{
    private static readonly TimeProvider Clock = new FixedTimeProvider();

    private static TransactionValues ValidValues() => new()
    {
        Source = SourceSystem.Harbour,
        ExternalId = "HB-100231",
        MerchantId = "M-2001",
        OccurredAtUtc = new DateTime(2026, 9, 1, 16, 15, 0, DateTimeKind.Utc),
        Amount = 25.50m,
        Currency = Currency.CAD,
        Type = TransactionType.Purchase
    };

    private static TransactionValues With(
        string? externalId = "HB-100231",
        string? merchantId = "M-2001",
        DateTime? occurredAtUtc = null,
        decimal? amount = 25.50m,
        Currency? currency = Currency.CAD,
        TransactionType? type = TransactionType.Purchase) => new()
    {
        Source = SourceSystem.Harbour,
        ExternalId = externalId,
        MerchantId = merchantId,
        OccurredAtUtc = occurredAtUtc ?? new DateTime(2026, 9, 1, 16, 15, 0, DateTimeKind.Utc),
        Amount = amount,
        Currency = currency,
        Type = type
    };

    [TestMethod]
    public void Test_Create_ValidValues_ReturnsTransaction()
    {
        var result = Transaction.Create(ValidValues(), Clock);

        Assert.IsTrue(result.IsSuccess);
        var transaction = result.Value;
        Assert.AreNotEqual(Guid.Empty, transaction.Id);
        Assert.AreEqual(SourceSystem.Harbour, transaction.Source);
        Assert.AreEqual("HB-100231", transaction.ExternalId);
        Assert.AreEqual("M-2001", transaction.MerchantId);
        Assert.AreEqual(25.50m, transaction.Amount);
        Assert.AreEqual(Currency.CAD, transaction.Currency);
        Assert.AreEqual(TransactionType.Purchase, transaction.Type);
        Assert.AreEqual(DateTimeKind.Utc, transaction.OccurredAtUtc.Kind);
    }

    [TestMethod]
    public void Test_Create_TrimsExternalId()
    {
        var result = Transaction.Create(With(externalId: "  HB-100231 "), Clock);

        Assert.AreEqual("HB-100231", result.Value.ExternalId);
    }

    [TestMethod]
    public void Test_Create_MissingValues_ReportsRequiredForEach()
    {
        var values = new TransactionValues();

        var result = Transaction.Create(values, Clock);

        Assert.IsFalse(result.IsSuccess);
        var fields = result.Error.Where(e => e.Code == ValidationCode.Required).Select(e => e.Field).ToList();
        CollectionAssert.AreEquivalent(
            new[]
            {
                TransactionField.Source, TransactionField.ExternalId, TransactionField.MerchantId,
                TransactionField.OccurredAtUtc, TransactionField.Amount, TransactionField.Currency,
                TransactionField.Type
            },
            fields);
    }

    [TestMethod]
    public void Test_Create_ExternalIdTooLong_IsRejected()
    {
        var result = Transaction.Create(With(externalId: new string('X', 51)), Clock);

        Assert.AreEqual(ValidationCode.TooLong, result.Error.Single().Code);
        Assert.AreEqual(TransactionField.ExternalId, result.Error.Single().Field);
    }

    [TestMethod]
    public void Test_Create_TimeInFuture_IsOutOfRange()
    {
        var future = FixedTimeProvider.DefaultNow.UtcDateTime.AddMinutes(1);

        var result = Transaction.Create(With(occurredAtUtc: future), Clock);

        Assert.AreEqual(ValidationCode.OutOfRange, result.Error.Single().Code);
        Assert.AreEqual(TransactionField.OccurredAtUtc, result.Error.Single().Field);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void Test_Create_AmountNotPositive_IsOutOfRange(int amount)
    {
        var result = Transaction.Create(With(amount: amount), Clock);

        Assert.AreEqual(ValidationCode.OutOfRange, result.Error.Single().Code);
        Assert.AreEqual(TransactionField.Amount, result.Error.Single().Field);
    }

    [TestMethod]
    public void Test_Create_AmountWithThreeDecimals_IsInvalid()
    {
        var result = Transaction.Create(With(amount: 10.001m), Clock);

        Assert.AreEqual(ValidationCode.Invalid, result.Error.Single().Code);
        Assert.AreEqual(TransactionField.Amount, result.Error.Single().Field);
    }

    [TestMethod]
    public void Test_Create_SeveralProblems_ReturnsAllErrors()
    {
        var result = Transaction.Create(
            With(externalId: null, merchantId: new string('M', 30), amount: -1m, currency: null), Clock);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(4, result.Error.Count);
        Assert.IsTrue(result.Error.Any(e => e is { Field: TransactionField.ExternalId, Code: ValidationCode.Required }));
        Assert.IsTrue(result.Error.Any(e => e is { Field: TransactionField.MerchantId, Code: ValidationCode.TooLong }));
        Assert.IsTrue(result.Error.Any(e => e is { Field: TransactionField.Amount, Code: ValidationCode.OutOfRange }));
        Assert.IsTrue(result.Error.Any(e => e is { Field: TransactionField.Currency, Code: ValidationCode.Required }));
    }
}
