using System.Globalization;
using TxnIngest.Core.Domain;
using TxnIngest.Core.Ingestion;

namespace TxnIngest.Core.Data;

/// <summary>A small set of earlier Harbour transactions, so duplicate handling can be seen straight away.</summary>
public static class SeedData
{
    private static readonly (string Id, string Merchant, string Time, decimal Amount, Currency Currency, TransactionType Type, string? Auth)[] Rows =
    [
        ("HB-100001", "M-2001", "2026-08-25T09:15:00Z", 45.00m, Currency.CAD, TransactionType.Purchase, "A10231"),
        ("HB-100002", "M-2001", "2026-08-25T12:30:00Z", 19.99m, Currency.USD, TransactionType.Purchase, "A10232"),
        ("HB-100003", "M-2002", "2026-08-25T14:05:00Z", 120.50m, Currency.CAD, TransactionType.Purchase, "A10233"),
        ("HB-100004", "M-2002", "2026-08-25T16:40:00Z", 12.50m, Currency.CAD, TransactionType.Refund, null),
        ("100450", "M-2001", "2026-08-25T18:20:00Z", 75.00m, Currency.USD, TransactionType.Purchase, "A10235")
    ];

    public static async Task LoadAsync(IngestDbContext db, TimeProvider clock)
    {
        var receipt = new FileReceipt
        {
            Token = Guid.NewGuid(),
            Source = SourceSystem.Harbour,
            FileName = "harbour_20260825.csv",
            Hash = "5B0C1E7A9D3F4826B1A7C6E0D4F98A3217CE5B6D0F8A24913C7E1B5D6A0F9C42",
            ReceivedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 8, 26, 6, 0, 0), DateTimeKind.Utc),
            ProcessedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 8, 26, 6, 0, 2), DateTimeKind.Utc),
            State = FileState.Completed,
            RowsReceived = Rows.Length,
            RowsAccepted = Rows.Length
        };
        db.FileReceipts.Add(receipt);

        for (var i = 0; i < Rows.Length; i++)
        {
            var row = Rows[i];
            var values = new TransactionValues
            {
                Source = SourceSystem.Harbour,
                ExternalId = row.Id,
                MerchantId = row.Merchant,
                OccurredAtUtc = DateTime.Parse(row.Time, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                Amount = row.Amount,
                Currency = row.Currency,
                Type = row.Type
            };
            if (row.Auth is not null)
                values.UnmappedValues["auth_code"] = row.Auth;

            var transaction = Transaction.Create(values, clock).Value;
            transaction.ReceiptToken = receipt.Token;
            db.Transactions.Add(transaction);
            db.RowResults.Add(new RowResult
            {
                ReceiptToken = receipt.Token,
                RowNumber = i + 1,
                ExternalId = row.Id,
                Accepted = true
            });
        }

        await db.SaveChangesAsync();
    }
}
