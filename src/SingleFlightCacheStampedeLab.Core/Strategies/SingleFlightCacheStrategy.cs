using SingleFlightCacheStampedeLab.Abstractions;
using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Cache;

namespace SingleFlightCacheStampedeLab.Strategies;

public sealed class SingleFlightCacheStrategy(
    IBackendLoader backend, InMemoryCacheStore store, TimeSpan ttl, TimeProvider? timeProvider = null)
    : CacheStrategyBase(backend, store, ttl, timeProvider)
{
    private readonly FlightRegistry<BackendValue> _flights = new();

    public override string Name => "SingleFlight";
    internal int ActiveFlights => _flights.Count;

    public override async ValueTask<BackendValue> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateRequest(key, cancellationToken);
        if (TryGetFresh(key, out BackendValue? value))
        {
            Diagnostics.Hit();
            return value;
        }

        Diagnostics.Miss();
        var candidate = new Flight<BackendValue>();
        Flight<BackendValue> flight = _flights.JoinOrCreate(key, candidate);
        bool waiter = !ReferenceEquals(flight, candidate);
        if (waiter)
        {
            flight.AddWaiter();
            Diagnostics.WaiterJoined();
        }
        else
        {
            Diagnostics.FlightCreated();
            _ = RunLeaderAsync(key, flight);
        }

        try
        {
            return await flight.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            if (waiter)
            {
                flight.RemoveWaiter();
                Diagnostics.WaiterLeft();
            }
        }
    }

    private async Task RunLeaderAsync(string key, Flight<BackendValue> flight)
    {
        try
        {
            // The previous flight may have populated the cache between our miss and election.
            if (!TryGetFresh(key, out BackendValue? value))
            {
                value = await Backend.LoadAsync(key, CancellationToken.None);
                Cache(key, value);
            }

            flight.Complete(value);
        }
        catch (Exception exception)
        {
            // Convert the backend failure into the explicit result shared by all callers.
            flight.Fail(exception);
        }
        finally
        {
            _flights.RemoveExact(key, flight);
        }
    }
}
