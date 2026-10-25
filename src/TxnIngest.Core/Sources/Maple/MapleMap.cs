using CsvHelper.Configuration;
using System.Globalization;

namespace TxnIngest.Core.Sources.Maple;

/// <summary>
/// Maps a Maple file onto <see cref="MapleRow"/> by header name, so column order does not matter.
/// </summary>
/// <remarks>Every column is required. A file that lacks one is not a Maple file.</remarks>
public sealed class MapleMap : ClassMap<MapleRow>
{
    public MapleMap()
    {
        Map(m => m.Ref).Name("ref");
        Map(m => m.MerchantId).Name("mid");

        // Assuming UTC for records. Would need to reference merchant otherwise potentially to know.
        Map(m => m.TransactionDate).Name("txn_date");
        Map(m => m.TransactionTime).Name("txn_time");

        Map(m => m.TransactionAmount).Name("txn_amt");
        Map(m => m.TransactionCode).Name("txn_code");
        Map(m => m.TerminalId).Name("terminal_id"); //Make sure this inserts the same way auth_code does.
    }
}
