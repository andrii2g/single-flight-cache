using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Cache;
using SingleFlightCacheStampedeLab.Tests.TestDoubles;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Cache;

public sealed class InMemoryCacheStoreTests
{
    [Fact]
    public void FreshUntilExactExpirationBoundary()
    {
        var time = new ManualTimeProvider();
        var store = new InMemoryCacheStore();
        BackendValue expected = BackendValue.Create("hot", 3);
        store.Set("hot", expected, time.GetUtcNow() + TimeSpan.FromSeconds(1));
        Assert.True(store.TryGetFresh("hot", time.GetUtcNow(), out BackendValue? actual));
        Assert.Equal(expected, actual);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.False(store.TryGetFresh("hot", time.GetUtcNow(), out _));
        Assert.False(store.Expire("hot"));
    }

    [Fact]
    public void ExpireAndClearRemoveValues()
    {
        var store = new InMemoryCacheStore();
        store.Set("a", BackendValue.Create("a", 0), DateTimeOffset.MaxValue);
        store.Set("b", BackendValue.Create("b", 0), DateTimeOffset.MaxValue);
        Assert.True(store.Expire("a"));
        Assert.False(store.TryGetFresh("a", DateTimeOffset.MinValue, out _));
        store.Clear();
        Assert.False(store.TryGetFresh("b", DateTimeOffset.MinValue, out _));
    }
}
