using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TxnIngest.Core.Domain;
using TxnIngest.Core.Ingestion;
using TxnIngest.Core.Responses;
using TxnIngest.Core.Tests.TestSupport;

namespace TxnIngest.Core.Tests.Responses;

[TestClass]
public class ResponseFileFormatterTests
{
    private TestDb _testDb = null!;

    [TestInitialize]
    public void Setup() => _testDb = new TestDb();

    [TestCleanup]
    public void Cleanup() => _testDb.Dispose();

    private async Task<ResponseFile> IngestAndFormatAsync(CsvBuilder csv, string fileName = "harbour_20260902.csv")
    {
        var db = _testDb.Context;
        var service = new IngestionService(db, new FixedTimeProvider());
        var receipt = await service.IngestAsync(SourceSystem.Harbour, csv.ToStream(), fileName);
        var rows = await db.RowResults.Where(r => r.ReceiptToken == receipt.Token).ToListAsync();
        return ResponseFileFormatter.Format(receipt, rows);
    }

    [TestMethod]
    public async Task Test_Format_ErrorsNameTheHarbourColumns()
    {
        var csv = new CsvBuilder()
            .Row(id: "HB-1", merchant: "", amount: "0", datetime: "bad", currency: "EUR", type: "VOID");

        var response = await IngestAndFormatAsync(csv);

        var columns = response.Records.Single().Errors.Select(e => e.Column).ToList();
        CollectionAssert.AreEquivalent(
            new[] { "merchant_id", "amount", "transaction_datetime", "currency", "transaction_type" },
            columns);
    }

    [TestMethod]
    public async Task Test_Format_RecordsAreInRowOrderWithStatuses()
    {
        var csv = new CsvBuilder().Row(id: "HB-1").Row(id: "HB-2", amount: "-1").Row(id: "HB-3");

        var response = await IngestAndFormatAsync(csv);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, response.Records.Select(r => r.Row).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Accepted", "Rejected", "Accepted" },
            response.Records.Select(r => r.Status).ToArray());
        Assert.AreEqual(0, response.Records[0].Errors.Count);
        Assert.AreEqual("HB-2", response.Records[1].TransactionId);
        Assert.AreEqual("Amount must be greater than 0.", response.Records[1].Errors.Single().Message);
    }

    [TestMethod]
    public async Task Test_Format_FailedFile_HasReasonAndNoRecords()
    {
        var csv = new CsvBuilder().WithHeader("a,b,c").Row(id: "HB-1");

        var response = await IngestAndFormatAsync(csv);

        Assert.AreEqual(FileState.Failed, response.State);
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.FileError));
        Assert.AreEqual(0, response.Records.Count);
    }

    [TestMethod]
    public async Task Test_Format_DuplicateFile_HasStateAndNoRecords()
    {
        var csv = new CsvBuilder().Row(id: "HB-1");
        await IngestAndFormatAsync(csv);

        var response = await IngestAndFormatAsync(csv);

        Assert.AreEqual(FileState.DuplicateFile, response.State);
        Assert.AreEqual(0, response.Records.Count);
    }

    [TestMethod]
    public async Task Test_ToJson_UsesCamelCaseNamesAndTextValues()
    {
        var csv = new CsvBuilder().Row(id: "HB-1").Row(id: "HB-2", amount: "0");

        var json = ResponseFileFormatter.ToJson(await IngestAndFormatAsync(csv));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.AreEqual("Completed", root.GetProperty("state").GetString());
        Assert.AreEqual(2, root.GetProperty("rowsReceived").GetInt32());
        Assert.IsFalse(root.TryGetProperty("fileError", out _));
        var records = root.GetProperty("records");
        Assert.AreEqual("HB-1", records[0].GetProperty("transactionId").GetString());
        Assert.AreEqual("Rejected", records[1].GetProperty("status").GetString());
        var error = records[1].GetProperty("errors")[0];
        Assert.AreEqual("amount", error.GetProperty("column").GetString());
        Assert.AreEqual("OutOfRange", error.GetProperty("code").GetString());
    }
}
