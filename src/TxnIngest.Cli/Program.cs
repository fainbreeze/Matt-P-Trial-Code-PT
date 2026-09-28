using System.CommandLine;
using TxnIngest.Cli.Commands;

var root = new RootCommand("Reads partner transaction files and reports what was accepted.");
root.Subcommands.Add(IngestCommand.Create());
root.Subcommands.Add(ReceiptsCommand.Create());
root.Subcommands.Add(DbCommand.Create());

var parsed = root.Parse(args);
if (parsed.Errors.Count > 0)
{
    foreach (var error in parsed.Errors)
        Console.Error.WriteLine(error.Message);
    return 2;
}

return await parsed.InvokeAsync();
