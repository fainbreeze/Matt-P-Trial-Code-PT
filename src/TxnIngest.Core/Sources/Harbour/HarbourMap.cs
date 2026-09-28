using CsvHelper.Configuration;

namespace TxnIngest.Core.Sources.Harbour;

/// <summary>
/// Maps a Harbour file onto <see cref="HarbourRow"/> by header name, so column order does not matter.
/// </summary>
/// <remarks>Every column is required. A file that lacks one is not a Harbour file.</remarks>
public sealed class HarbourMap : ClassMap<HarbourRow>
{
    public HarbourMap()
    {
        Map(m => m.TransactionId).Name("transaction_id");
        Map(m => m.MerchantId).Name("merchant_id");
        Map(m => m.TransactionDatetime).Name("transaction_datetime");
        Map(m => m.Amount).Name("amount");
        Map(m => m.Currency).Name("currency");
        Map(m => m.TransactionType).Name("transaction_type");
        Map(m => m.AuthCode).Name("auth_code");
    }
}
