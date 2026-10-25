using System.Globalization;
using System.Security.Cryptography;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using TxnIngest.Core.Data;
using TxnIngest.Core.Domain;
using TxnIngest.Core.Sources.Harbour;
using TxnIngest.Core.Sources.Maple;

namespace TxnIngest.Core.Ingestion;

/// <summary>Reads a partner file, validates each row, stores the good ones, and records what happened.</summary>
public sealed class IngestionService(IngestDbContext db, TimeProvider clock)
{
    private readonly HarbourAdapter _harbourAdapter = new();
    private readonly MapleAdapter _mapleAdapter = new();

    public async Task<FileReceipt> IngestAsync(
        SourceSystem source, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var receipt = new FileReceipt
        {
            Token = Guid.NewGuid(),
            Source = source,
            FileName = fileName,
            Hash = Convert.ToHexString(SHA256.HashData(bytes)),
            ReceivedAtUtc = clock.GetUtcNow().UtcDateTime,
            State = FileState.Received
        };

        if (await db.FileReceipts.AnyAsync(r => r.Hash == receipt.Hash, cancellationToken))
        {
            receipt.State = FileState.DuplicateFile;
            return await FinishAsync(receipt, [], cancellationToken);
        }

        List<(int Number, TransactionValues Values)> candidates;

        //Consider moving the logic inside these statements to functions.
        if (source == SourceSystem.Harbour)
        {
            List<HarbourRow> rows;
            try
            {
                rows = ReadHarbourRows(bytes);
            }
            catch (CsvHelperException ex)
            {
                receipt.State = FileState.Failed;
                receipt.FailureReason = FirstLine(ex.Message);
                return await FinishAsync(receipt, [], cancellationToken);
            }

            candidates = rows
                .Select((row, index) => (Number: index + 1, Values: _harbourAdapter.ToValues(row)))
                .ToList();
        }

        else if (source == SourceSystem.Maple)
        {
            List<MapleRow> rows;
            try
            {
                rows = ReadMapleRows(bytes);
            }
            catch (CsvHelperException ex)
            {
                receipt.State = FileState.Failed;
                receipt.FailureReason = FirstLine(ex.Message);
                return await FinishAsync(receipt, [], cancellationToken);
            }

            //Since Maple can correct rows later, we reverse the order so that the last row is the one that is kept if there are duplicates.
            candidates = rows
                .Select((row, index) => (Number: index + 1, Values: _mapleAdapter.ToValues(row)))
                .Reverse()
                .ToList();
        }

        else
        {
            //Cannot parse a source we don't support.
            receipt.State = FileState.Failed;
            receipt.FailureReason = "Unable to parse unsupported source. Currently only supports Harbour and Maple.";
            return await FinishAsync(receipt, [], cancellationToken);
        }

        var storedIds = await FindStoredIdsAsync(candidates.Select(c => c.Values.ExternalId), cancellationToken);
        var acceptedIds = new HashSet<string>();
        var results = new List<RowResult>();
        var accepted = new List<Transaction>();

        foreach (var (number, values) in candidates)
        {
            var errors = new List<ValidationError>();
            var created = Transaction.Create(values, clock);
            if (!created.IsSuccess)
                errors.AddRange(created.Error);

            var externalId = values.ExternalId;
            
            if (!string.IsNullOrEmpty(externalId) && (storedIds.Contains(externalId) || acceptedIds.Contains(externalId)))
            {
                errors.Add(new ValidationError(TransactionField.ExternalId, ValidationCode.Duplicate,
                    "A transaction with this ID has already been received."));
            }

            if (errors.Count == 0)
            {
                created.Value.ReceiptToken = receipt.Token;
                accepted.Add(created.Value);
                acceptedIds.Add(created.Value.ExternalId);
            }

            results.Add(new RowResult
            {
                ReceiptToken = receipt.Token,
                RowNumber = number,
                ExternalId = externalId,
                Accepted = errors.Count == 0,
                Errors = errors
            });
        }

        receipt.State = FileState.Completed;
        receipt.RowsReceived = results.Count;
        receipt.RowsAccepted = accepted.Count;
        receipt.RowsRejected = results.Count - accepted.Count;

        db.Transactions.AddRange(accepted);
        return await FinishAsync(receipt, results, cancellationToken);
    }

    private static List<HarbourRow> ReadHarbourRows(byte[] bytes)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var reader = new StreamReader(new MemoryStream(bytes));
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<HarbourMap>();
        return csv.GetRecords<HarbourRow>().ToList();
    }

    private static List<MapleRow> ReadMapleRows(byte[] bytes)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var reader = new StreamReader(new MemoryStream(bytes));
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<MapleMap>();
        return csv.GetRecords<MapleRow>().ToList();
    }

    private async Task<HashSet<string>> FindStoredIdsAsync(IEnumerable<string?> ids, CancellationToken cancellationToken)
    {
        var wanted = ids.OfType<string>().Where(id => id.Length > 0).Distinct().ToList();
        var found = await db.Transactions
            .Where(t => wanted.Contains(t.ExternalId))
            .Select(t => t.ExternalId)
            .ToListAsync(cancellationToken);
        return found.ToHashSet();
    }

    private async Task<FileReceipt> FinishAsync(
        FileReceipt receipt, IReadOnlyList<RowResult> results, CancellationToken cancellationToken)
    {
        receipt.ProcessedAtUtc = clock.GetUtcNow().UtcDateTime;
        db.FileReceipts.Add(receipt);
        db.RowResults.AddRange(results);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return receipt;
    }

    private static string FirstLine(string message)
        => message.Split('\n', 2)[0].Trim();
}
