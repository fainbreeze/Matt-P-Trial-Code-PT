using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TxnIngest.Core.Domain;
using TxnIngest.Core.Sources.Harbour;

namespace TxnIngest.Core.Sources.Maple
{
    /// <summary>
    /// Converts between the Maple columns and our own names, in both directions. No validation.
    /// </summary>
    public sealed class MapleAdapter
    {
        public TransactionValues ToValues(MapleRow row)
        {
            var values = new TransactionValues
            {
                Source = SourceSystem.Maple,
                ExternalId = ExternalIdFor(row),
                MerchantId = row.MerchantId,
                OccurredAtUtc = ReadDateTime(row.TransactionDate + row.TransactionTime),
                Amount = ReadAmount(row.TransactionAmount),
                Currency = Currency.CAD, //PLACEHOLDER FOR NOW, NEED TO FIND A WAY TO PULL THE MERCHANT'S CURRENCY.
                Type = ReadCode(row.TransactionCode)
            };

            //Value that is kept but unused.
            AddUnmappedValue(values, "terminal_id", row.TerminalId);

            return values;
        }

        /// <summary>
        /// Trimmed, because duplicate detection compares identifiers exactly and padding would otherwise let a
        /// resent transaction through as new.
        /// </summary>
        public string? ExternalIdFor(MapleRow row) => row.Ref?.Trim();

        /// <summary>
        /// Returns UTC. A time with an offset is converted, and a time with none is taken as UTC. Text that will
        /// not read becomes null, and <see cref="Transaction.Create"/> then reports the value as missing.
        /// </summary>
        private static DateTime? ReadDateTime(string? text)
            => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                   ? parsed.UtcDateTime
                   : null;

        private static decimal? ReadAmount(string? text)
        => decimal.TryParse(text,
                            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                            CultureInfo.InvariantCulture,
                            out var parsed)
               ? parsed
               : null;

        private static TransactionType? ReadCode(string? text) => text?.Trim().ToUpperInvariant() switch
        {
            "P" => TransactionType.Purchase,
            "R" => TransactionType.Refund,
            "V" => TransactionType.Void,
            _ => null
        };

        private static void AddUnmappedValue(TransactionValues values, string column, string? text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                values.UnmappedValues[column] = text;
        }

    }
}
