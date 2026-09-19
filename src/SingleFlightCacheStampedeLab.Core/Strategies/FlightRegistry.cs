using System.Collections.Concurrent;

namespace SingleFlightCacheStampedeLab.Strategies;

internal sealed class FlightRegistry<T>
{
    private readonly ConcurrentDictionary<string, Flight<T>> _flights = new(StringComparer.Ordinal);

    public int Count => _flights.Count;

    public Flight<T> JoinOrCreate(string key, Flight<T> candidate)
    {
        while (true)
        {
            // No value factory: construction is inert; only the insertion winner starts work.
            Flight<T> flight = _flights.GetOrAdd(key, candidate);
            if (ReferenceEquals(flight, candidate) || !flight.Task.IsCompleted) return flight;

            // A retry need not wait for an old leader's finally block.
            RemoveExact(key, flight);
        }
    }

    public bool RemoveExact(string key, Flight<T> flight)
        => ((ICollection<KeyValuePair<string, Flight<T>>>)_flights).Remove(new(key, flight));
}
