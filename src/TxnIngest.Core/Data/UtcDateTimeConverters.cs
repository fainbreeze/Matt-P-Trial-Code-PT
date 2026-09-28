using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TxnIngest.Core.Data;

/// <summary>SQLite drops the kind of a stored date, so it is set back to UTC when read.</summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

public sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    value => value == null ? null : value.Value.ToUniversalTime(),
    value => value == null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
