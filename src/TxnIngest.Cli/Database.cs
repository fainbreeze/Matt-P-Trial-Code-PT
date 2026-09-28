using TxnIngest.Core.Data;

namespace TxnIngest.Cli;

internal static class Database
{
    public const string PathVariable = "TXNINGEST_DB";

    public static string Path => Environment.GetEnvironmentVariable(PathVariable) is { Length: > 0 } configured
        ? configured
        : System.IO.Path.Combine(Directory.GetCurrentDirectory(), "txningest.db");

    /// <summary>Opens the database file, creating it on first use.</summary>
    public static IngestDbContext Open()
    {
        var db = IngestDbContext.ForFile(Path);
        db.Database.EnsureCreated();
        return db;
    }
}
