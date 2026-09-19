namespace SingleFlightCacheStampedeLab.Utilities;

internal static class DeterministicShuffle
{
    public const int Seed = 1729;

    public static void Shuffle<T>(T[] items)
    {
        // Fixed xorshift32 + Fisher-Yates: order does not depend on System.Random's runtime version.
        uint state = Seed;
        for (int i = items.Length - 1; i > 0; i--)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            int j = (int)(state % (uint)(i + 1));
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
