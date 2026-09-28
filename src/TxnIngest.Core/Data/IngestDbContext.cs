using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TxnIngest.Core.Domain;
using TxnIngest.Core.Ingestion;

namespace TxnIngest.Core.Data;

public sealed class IngestDbContext(DbContextOptions<IngestDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    public DbSet<FileReceipt> FileReceipts => Set<FileReceipt>();
    public DbSet<RowResult> RowResults => Set<RowResult>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public static IngestDbContext ForFile(string path)
        => new(new DbContextOptionsBuilder<IngestDbContext>().UseSqlite($"Data Source={path}").Options);

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<SourceSystem>().HaveConversion<string>();
        builder.Properties<Currency>().HaveConversion<string>();
        builder.Properties<TransactionType>().HaveConversion<string>();
        builder.Properties<FileState>().HaveConversion<string>();
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<FileReceipt>(e =>
        {
            e.HasKey(r => r.Token);
            e.HasIndex(r => r.Hash);
        });

        model.Entity<RowResult>(e =>
        {
            e.HasIndex(r => r.ReceiptToken);
            HasJsonConversion(e.Property(r => r.Errors));
        });

        model.Entity<Transaction>(e =>
        {
            e.HasIndex(t => t.ExternalId).IsUnique();
            HasJsonConversion(e.Property(t => t.UnmappedValues));
        });
    }

    private static string ToJson<T>(T value) => JsonSerializer.Serialize(value, Json);

    private static T FromJson<T>(string text) where T : new() => JsonSerializer.Deserialize<T>(text, Json) ?? new T();

    private static void HasJsonConversion<T>(PropertyBuilder<T> property) where T : class, new()
        => property.HasConversion(
            value => ToJson(value),
            text => FromJson<T>(text),
            new ValueComparer<T>(
                (a, b) => ToJson(a) == ToJson(b),
                value => ToJson(value).GetHashCode(),
                value => FromJson<T>(ToJson(value))));
}
