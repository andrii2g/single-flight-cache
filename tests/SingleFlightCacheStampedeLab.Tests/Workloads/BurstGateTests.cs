using SingleFlightCacheStampedeLab.Workloads;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Workloads;

public sealed class BurstGateTests
{
    [Fact]
    public async Task AllCallersMustArriveAndNonePassesBeforeRelease()
    {
        using var gate = new BurstGate(3);
        Task first = gate.ArriveAndWaitAsync(TestContext.Current.CancellationToken);
        Task second = gate.ArriveAndWaitAsync(TestContext.Current.CancellationToken);
        Assert.False(gate.Ready.IsCompleted);
        Assert.Equal(2, gate.ReadyCount);
        Assert.Throws<InvalidOperationException>(gate.Release);
        Task third = gate.ArriveAndWaitAsync(TestContext.Current.CancellationToken);
        await gate.Ready.WaitAsync(TestContext.Current.CancellationToken);
        Assert.All(new[] { first, second, third }, task => Assert.False(task.IsCompleted));
        gate.Release();
        await Task.WhenAll(first, second, third);
    }

    [Fact]
    public async Task ParticipantCancellationAbortsCoordinatorAndPeers()
    {
        using var cancellation = new CancellationTokenSource();
        using var gate = new BurstGate(3);
        Task first = gate.ArriveAndWaitAsync(cancellation.Token);
        Task second = gate.ArriveAndWaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.Ready);
    }

    [Fact]
    public async Task CancellationAfterReadyStillReleasesEveryParticipant()
    {
        using var cancellation = new CancellationTokenSource();
        using var gate = new BurstGate(2, cancellation.Token);
        Task first = gate.ArriveAndWaitAsync(TestContext.Current.CancellationToken);
        Task second = gate.ArriveAndWaitAsync(TestContext.Current.CancellationToken);
        await gate.Ready.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        gate.Release();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
    }
    [Fact]
    public async Task CoordinatorCancellationWorksBeforeAnyCallerArrives()
    {
        using var cancellation = new CancellationTokenSource();
        using var gate = new BurstGate(3, cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.Ready);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.ArriveAndWaitAsync(TestContext.Current.CancellationToken));
    }
}
