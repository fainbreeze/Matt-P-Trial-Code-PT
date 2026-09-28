namespace TxnIngest.Core.Tests.TestSupport;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider() : this(DefaultNow)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}
