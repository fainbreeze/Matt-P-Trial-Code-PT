using Microsoft.EntityFrameworkCore;
using TxnIngest.Core.Data;
using TxnIngest.Core.Domain;
using TxnIngest.Core.Ingestion;
using TxnIngest.Core.Tests.TestSupport;

namespace TxnIngest.Core.Tests.Ingestion;

[TestClass]
public class IngestionServiceTests
{
    private TestDb _testDb = null!;

    private IngestDbContext Db => _testDb.Context;

    [TestInitialize]
    public void Setup() => _testDb = new TestDb();

    [TestCleanup]
    public void Cleanup() => _testDb.Dispose();

    private Task<FileReceipt> IngestAsync(CsvBuilder csv, string fileName = "harbour_20260901.csv")
    {
        var service = new IngestionService(Db, new FixedTimeProvider());
        return service.IngestAsync(SourceSystem.Harbour, csv.ToStream(), fileName);
    }

    [TestMethod]
    public async Task Test_Ingest_AllRowsValid_AcceptsEveryRow()
    {
        var csv = new CsvBuilder()
            .Row(id: "HB-1")
            .Row(id: "HB-2", type: "REFUND")
            .Row(id: "HB-3", currency: "USD");

        var receipt = await IngestAsync(csv);

        Assert.AreEqual(FileState.Completed, receipt.State);
        Assert.AreEqual(3, receipt.RowsReceived);
        Assert.AreEqual(3, receipt.RowsAccepted);
        Assert.AreEqual(0, receipt.RowsRejected);
        Assert.AreEqual(3, await Db.Transactions.CountAsync());
        Assert.IsTrue(await Db.RowResults.AllAsync(r => r.Accepted));
    }

    [TestMethod]
    public async Task Test_Ingest_MixedRows_CountsAcceptedAndRejected()
    {
        var csv = new CsvBuilder()
            .Row(id: "HB-1")
            .Row(id: "HB-2", amount: "0")
            .Row(id: "HB-3")
            .Row(id: "HB-4", datetime: "not a date");

        var receipt = await IngestAsync(csv);

        Assert.AreEqual(FileState.Completed, receipt.State);
        Assert.AreEqual(4, receipt.RowsReceived);
        Assert.AreEqual(2, receipt.RowsAccepted);
        Assert.AreEqual(2, receipt.RowsRejected);

        var results = await Db.RowResults.OrderBy(r => r.RowNumber).ToListAsync();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, results.Select(r => r.RowNumber).ToArray());
        CollectionAssert.AreEqual(new[] { true, false, true, false }, results.Select(r => r.Accepted).ToArray());
        Assert.AreEqual(ValidationCode.OutOfRange, results[1].Errors.Single().Code);
        Assert.AreEqual(ValidationCode.Required, results[3].Errors.Single().Code);
    }

    [TestMethod]
    public async Task Test_Ingest_WrongHeader_FailsWholeFileAndSavesNothing()
    {
        var csv = new CsvBuilder()
            .WithHeader("id,merchant,when,amount,currency,type,auth")
            .Row(id: "HB-1");

        var receipt = await IngestAsync(csv);

        Assert.AreEqual(FileState.Failed, receipt.State);
        Assert.IsFalse(string.IsNullOrWhiteSpace(receipt.FailureReason));
        Assert.AreEqual(0, await Db.Transactions.CountAsync());
        Assert.AreEqual(0, await Db.RowResults.CountAsync());
        Assert.AreEqual(1, await Db.FileReceipts.CountAsync());
    }

    [TestMethod]
    public async Task Test_Ingest_MalformedRow_FailsWholeFile()
    {
        var csv = new CsvBuilder().Row(id: "HB-1").Row(id: "HB-2", merchant: "\"unclosed");

        var receipt = await IngestAsync(csv);

        Assert.AreEqual(FileState.Failed, receipt.State);
        Assert.AreEqual(0, await Db.Transactions.CountAsync());
    }

    [TestMethod]
    public async Task Test_Ingest_SameFileTwice_SecondIsDuplicateFile()
    {
        var csv = new CsvBuilder().Row(id: "HB-1").Row(id: "HB-2");
        var first = await IngestAsync(csv);

        var second = await IngestAsync(csv, "harbour_resent.csv");

        Assert.AreEqual(FileState.Completed, first.State);
        Assert.AreEqual(FileState.DuplicateFile, second.State);
        Assert.AreNotEqual(first.Token, second.Token);
        Assert.AreEqual(first.Hash, second.Hash);
        Assert.AreEqual(2, await Db.Transactions.CountAsync());
        Assert.AreEqual(2, await Db.FileReceipts.CountAsync());
    }

    [TestMethod]
    public async Task Test_Ingest_IdAlreadyStored_RowIsRejectedAsDuplicate()
    {
        await IngestAsync(new CsvBuilder().Row(id: "HB-1"));

        var receipt = await IngestAsync(new CsvBuilder().Row(id: "HB-1", amount: "99.00").Row(id: "HB-2"));

        Assert.AreEqual(1, receipt.RowsAccepted);
        Assert.AreEqual(1, receipt.RowsRejected);
        var rejected = await Db.RowResults.SingleAsync(r => r.ReceiptToken == receipt.Token && !r.Accepted);
        Assert.AreEqual("HB-1", rejected.ExternalId);
        var error = rejected.Errors.Single();
        Assert.AreEqual(TransactionField.ExternalId, error.Field);
        Assert.AreEqual(ValidationCode.Duplicate, error.Code);
        Assert.AreEqual(25.00m, (await Db.Transactions.SingleAsync(t => t.ExternalId == "HB-1")).Amount);
    }

    [TestMethod]
    public async Task Test_Ingest_IdRepeatedInFile_SecondRowIsRejected()
    {
        var csv = new CsvBuilder().Row(id: "HB-1").Row(id: "HB-2").Row(id: "HB-1");

        var receipt = await IngestAsync(csv);

        Assert.AreEqual(2, receipt.RowsAccepted);
        Assert.AreEqual(1, receipt.RowsRejected);
        var results = await Db.RowResults.OrderBy(r => r.RowNumber).ToListAsync();
        Assert.IsTrue(results[0].Accepted);
        Assert.IsFalse(results[2].Accepted);
        Assert.AreEqual(ValidationCode.Duplicate, results[2].Errors.Single().Code);
    }

    [TestMethod]
    public async Task Test_Ingest_AcceptedRows_AreStoredWithReceiptTokenAndValues()
    {
        var csv = new CsvBuilder().Row(
            id: "HB-1", merchant: "M-9", datetime: "2026-09-01T23:30:00-07:00",
            amount: "10.25", currency: "usd", type: "REFUND", authCode: "Z9");

        var receipt = await IngestAsync(csv);

        var stored = await Db.Transactions.SingleAsync();
        Assert.AreEqual(receipt.Token, stored.ReceiptToken);
        Assert.AreEqual(SourceSystem.Harbour, stored.Source);
        Assert.AreEqual("M-9", stored.MerchantId);
        Assert.AreEqual(new DateTime(2026, 9, 2, 6, 30, 0), stored.OccurredAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, stored.OccurredAtUtc.Kind);
        Assert.AreEqual(10.25m, stored.Amount);
        Assert.AreEqual(Currency.USD, stored.Currency);
        Assert.AreEqual(TransactionType.Refund, stored.Type);
        Assert.AreEqual("Z9", stored.UnmappedValues["auth_code"]);
    }
}
