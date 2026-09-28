using System.Text;

namespace TxnIngest.Core.Tests.TestSupport;

/// <summary>Builds the text of a Harbour file.</summary>
public sealed class CsvBuilder
{
    public const string Header = "transaction_id,merchant_id,transaction_datetime,amount,currency,transaction_type,auth_code";

    private readonly List<string> _lines = [];
    private string _header = Header;

    public CsvBuilder WithHeader(string header)
    {
        _header = header;
        return this;
    }

    public CsvBuilder Row(
        string? id = "HB-200001",
        string? merchant = "M-3001",
        string? datetime = "2026-09-01T09:15:00-07:00",
        string? amount = "25.00",
        string? currency = "CAD",
        string? type = "SALE",
        string? authCode = "A20001")
    {
        _lines.Add(string.Join(',', id, merchant, datetime, amount, currency, type, authCode));
        return this;
    }

    public string Build() => string.Join('\n', [_header, .. _lines]) + "\n";

    public Stream ToStream() => new MemoryStream(Encoding.UTF8.GetBytes(Build()));
}
