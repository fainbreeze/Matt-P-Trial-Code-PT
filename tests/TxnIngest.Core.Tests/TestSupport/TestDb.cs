using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TxnIngest.Core.Data;

namespace TxnIngest.Core.Tests.TestSupport;

/// <summary>A database on an open in-memory connection. Each instance is empty and private to one test.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestDb()
    {
        _connection.Open();
        Context = new IngestDbContext(new DbContextOptionsBuilder<IngestDbContext>().UseSqlite(_connection).Options);
        Context.Database.EnsureCreated();
    }

    public IngestDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
