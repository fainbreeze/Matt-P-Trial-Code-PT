using CsvHelper.Configuration;

namespace TxnIngest.Core.Sources.Maple;

/// <summary>
/// Maps a Maple file onto <see cref="MapleRow"/> by header name, so column order does not matter.
/// </summary>
/// <remarks>Every column is required. A file that lacks one is not a Maple file.</remarks>
public sealed class MapleMap : ClassMap<MapleRow>
{
    public MapleMap()
    {
        Map(m => m.Ref).Name("transaction_id");
        Map(m => m.MerchantId).Name("merchant_id");
        Map(m => m.TransactionDate+m.TransactionTime+"Z").Name("transaction_datetime"); //Assuming UTC time for records. Would need to reference merchant otherwise potentially.
        Map(m => m.TransactionAmount).Name("amount");
        //Map(m => m.Currency).Name("currency"); //Reference merchant for currency?
        Map(m => m.TransactionCode).Name("transaction_type");
        Map(m => m.TerminalId).Name("terminal_id"); //Make sure this inserts the same way auth_code does.
    }
}
