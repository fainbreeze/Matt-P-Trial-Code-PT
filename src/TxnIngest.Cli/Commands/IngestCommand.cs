using System.CommandLine;
using TxnIngest.Core.Domain;
using TxnIngest.Core.Ingestion;

namespace TxnIngest.Cli.Commands;

internal static class IngestCommand
{
    public static Command Create()
    {
        var source = new Option<string>("--source") { Description = "The partner that sent the file, for example harbour.", Required = true };
        var file = new Option<string>("--file") { Description = "Path to the file to ingest.", Required = true };

        var command = new Command("ingest", "Reads one file, stores the accepted rows, and prints a summary.");
        command.Options.Add(source);
        command.Options.Add(file);
        command.SetAction((parsed, cancellationToken) => RunAsync(
            parsed.GetValue(source)!, parsed.GetValue(file)!, cancellationToken));
        return command;
    }

    private static async Task<int> RunAsync(string sourceName, string path, CancellationToken cancellationToken)
    {
        SourceSystem? source = sourceName.Trim().ToLowerInvariant() switch
        {
            "harbour" => SourceSystem.Harbour,
            "maple" => SourceSystem.Maple,
            _ => null
        };

        if (source is null)
        {
            Console.Error.WriteLine($"Unknown source '{sourceName}'.");
            return 2;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 2;
        }

        await using var db = Database.Open();
        await using var stream = File.OpenRead(path);
        var service = new IngestionService(db, TimeProvider.System);

        var receipt = await service.IngestAsync(source.Value, stream, Path.GetFileName(path), cancellationToken);

        Console.WriteLine(
            $"{receipt.Token} {receipt.State} received={receipt.RowsReceived} accepted={receipt.RowsAccepted} rejected={receipt.RowsRejected}");

        switch (receipt.State)
        {
            case FileState.DuplicateFile:
                Console.WriteLine("This file was already received. Nothing was stored.");
                break;
            case FileState.Failed:
                Console.Error.WriteLine($"File failed: {receipt.FailureReason}");
                break;
        }

        return receipt.State == FileState.Failed ? 1 : 0;
    }
}
