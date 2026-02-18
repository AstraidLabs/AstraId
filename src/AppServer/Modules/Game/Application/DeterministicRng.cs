namespace AppServer.Modules.Game.Application;

public interface IDeterministicRng
{
    ulong NextUInt64();
    double NextDouble();
    int NextInt(int minInclusive, int maxExclusive);
}

public class Xoshiro256StarStar : IDeterministicRng
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public Xoshiro256StarStar(ulong seed)
    {
        var sm = new SplitMix64(seed);
        _s0 = sm.Next();
        _s1 = sm.Next();
        _s2 = sm.Next();
        _s3 = sm.Next();
    }

    public ulong NextUInt64()
    {
        var result = RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = RotateLeft(_s3, 45);

        return result;
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        return minInclusive + (int)(NextUInt64() % (uint)(maxExclusive - minInclusive));
    }

    private static ulong RotateLeft(ulong x, int k) => (x << k) | (x >> (64 - k));

    private sealed class SplitMix64(ulong seed)
    {
        private ulong _value = seed;
        public ulong Next()
        {
            _value += 0x9E3779B97F4A7C15;
            var z = _value;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return z ^ (z >> 31);
        }
    }
}
