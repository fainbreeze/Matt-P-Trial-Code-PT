using System.CommandLine;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TxnIngest.Core.Responses;

namespace TxnIngest.Cli.Commands;

internal static class ReceiptsCommand
{
    public static Command Create()
    {
        var token = new Argument<Guid>("token") { Description = "The receipt token, as printed by ingest." };

        var list = new Command("list", "Lists all received files, newest first.");
        list.SetAction((_, cancellationToken) => ListAsync(cancellationToken));

        var show = new Command("show", "Prints the response file for one receipt.");
        show.Arguments.Add(token);
        show.SetAction((parsed, cancellationToken) => ShowAsync(parsed.GetValue(token), cancellationToken));

        var command = new Command("receipts", "Looks at files that were received.");
        command.Subcommands.Add(list);
        command.Subcommands.Add(show);
        return command;
    }

    private static async Task<int> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = Database.Open();
        var receipts = await db.FileReceipts
            .OrderByDescending(r => r.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        Console.WriteLine($"{"Token",-36}  {"Received (UTC)",-19}  {"Source",-8}  {"File",-28}  {"State",-13}  {"Rows",5}  {"Acc",5}  {"Rej",5}");
        foreach (var r in receipts)
        {
            var received = r.ReceivedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            Console.WriteLine(
                $"{r.Token,-36}  {received,-19}  {r.Source,-8}  {r.FileName,-28}  {r.State,-13}  {r.RowsReceived,5}  {r.RowsAccepted,5}  {r.RowsRejected,5}");
        }

        return 0;
    }

    private static async Task<int> ShowAsync(Guid token, CancellationToken cancellationToken)
    {
        await using var db = Database.Open();
        var receipt = await db.FileReceipts.FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
        if (receipt is null)
        {
            Console.Error.WriteLine($"No receipt with token {token}.");
            return 2;
        }

        var rows = await db.RowResults.Where(r => r.ReceiptToken == token).ToListAsync(cancellationToken);
        Console.WriteLine(ResponseFileFormatter.ToJson(ResponseFileFormatter.Format(receipt, rows)));
        return 0;
    }
}
