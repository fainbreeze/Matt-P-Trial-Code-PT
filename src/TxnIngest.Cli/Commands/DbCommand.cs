using System.CommandLine;
using TxnIngest.Core.Data;

namespace TxnIngest.Cli.Commands;

internal static class DbCommand
{
    public static Command Create()
    {
        var seed = new Option<bool>("--seed") { Description = "Also load a few earlier Harbour transactions." };

        var reset = new Command("reset", "Deletes and recreates the database.");
        reset.Options.Add(seed);
        reset.SetAction(parsed => ResetAsync(parsed.GetValue(seed)));

        var command = new Command("db", "Manages the local database.");
        command.Subcommands.Add(reset);
        return command;
    }

    private static async Task<int> ResetAsync(bool seed)
    {
        await using var db = IngestDbContext.ForFile(Database.Path);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        if (seed)
            await SeedData.LoadAsync(db, TimeProvider.System);

        Console.WriteLine($"Database reset{(seed ? " and seeded" : "")}: {Database.Path}");
        return 0;
    }
}
