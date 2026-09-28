using TxnIngest.Core.Domain;
using TxnIngest.Core.Sources.Harbour;

namespace TxnIngest.Core.Tests.Sources;

[TestClass]
public class HarbourAdapterTests
{
    private readonly HarbourAdapter _adapter = new();

    private static HarbourRow Row(
        string? id = "HB-100231",
        string? merchant = "M-2001",
        string? datetime = "2026-09-01T09:15:00-07:00",
        string? amount = "25.50",
        string? currency = "CAD",
        string? type = "SALE",
        string? authCode = "A20001") => new()
    {
        TransactionId = id,
        MerchantId = merchant,
        TransactionDatetime = datetime,
        Amount = amount,
        Currency = currency,
        TransactionType = type,
        AuthCode = authCode
    };

    [TestMethod]
    public void Test_ToValues_MapsEveryColumn()
    {
        var values = _adapter.ToValues(Row());

        Assert.AreEqual(SourceSystem.Harbour, values.Source);
        Assert.AreEqual("HB-100231", values.ExternalId);
        Assert.AreEqual("M-2001", values.MerchantId);
        Assert.AreEqual(new DateTime(2026, 9, 1, 16, 15, 0, DateTimeKind.Utc), values.OccurredAtUtc);
        Assert.AreEqual(25.50m, values.Amount);
        Assert.AreEqual(Currency.CAD, values.Currency);
        Assert.AreEqual(TransactionType.Purchase, values.Type);
    }

    [TestMethod]
    public void Test_ToValues_ConvertsOffsetTimeToUtc()
    {
        var values = _adapter.ToValues(Row(datetime: "2026-09-01T23:30:00-07:00"));

        Assert.AreEqual(new DateTime(2026, 9, 2, 6, 30, 0), values.OccurredAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, values.OccurredAtUtc!.Value.Kind);
    }

    [TestMethod]
    public void Test_ToValues_UnreadableValues_BecomeNull()
    {
        var values = _adapter.ToValues(Row(datetime: "yesterday", amount: "12,x", currency: "EUR", type: "VOID"));

        Assert.IsNull(values.OccurredAtUtc);
        Assert.IsNull(values.Amount);
        Assert.IsNull(values.Currency);
        Assert.IsNull(values.Type);
    }

    [TestMethod]
    public void Test_ToValues_SaleIsPurchaseAndRefundIsRefund()
    {
        Assert.AreEqual(TransactionType.Purchase, _adapter.ToValues(Row(type: "SALE")).Type);
        Assert.AreEqual(TransactionType.Refund, _adapter.ToValues(Row(type: "REFUND")).Type);
    }

    [TestMethod]
    public void Test_ToValues_NumericCurrencyIsNotAccepted()
    {
        Assert.IsNull(_adapter.ToValues(Row(currency: "1")).Currency);
    }

    [TestMethod]
    public void Test_ToValues_AmountUsesInvariantCulture()
    {
        Assert.AreEqual(1234.56m, _adapter.ToValues(Row(amount: "1234.56")).Amount);
        Assert.IsNull(_adapter.ToValues(Row(amount: "1,234.56")).Amount);
    }

    [TestMethod]
    public void Test_ToValues_AuthCodeIsKeptUnmapped()
    {
        var values = _adapter.ToValues(Row(authCode: "A20001"));

        Assert.AreEqual("A20001", values.UnmappedValues["auth_code"]);
    }

    [TestMethod]
    public void Test_ExternalIdFor_TrimsPadding()
    {
        Assert.AreEqual("HB-100231", _adapter.ExternalIdFor(Row(id: "  HB-100231  ")));
        Assert.IsNull(_adapter.ExternalIdFor(Row(id: null)));
    }

    [TestMethod]
    public void Test_ColumnFor_KnownField_ReturnsColumnName()
    {
        Assert.AreEqual("transaction_id", _adapter.ColumnFor(TransactionField.ExternalId));
        Assert.AreEqual("merchant_id", _adapter.ColumnFor(TransactionField.MerchantId));
        Assert.AreEqual("transaction_datetime", _adapter.ColumnFor(TransactionField.OccurredAtUtc));
        Assert.AreEqual("amount", _adapter.ColumnFor(TransactionField.Amount));
        Assert.AreEqual("currency", _adapter.ColumnFor(TransactionField.Currency));
        Assert.AreEqual("transaction_type", _adapter.ColumnFor(TransactionField.Type));
    }

    [TestMethod]
    public void Test_ColumnFor_UnknownField_ReturnsFieldName()
    {
        Assert.AreEqual("SomethingElse", _adapter.ColumnFor("SomethingElse"));
    }
}
