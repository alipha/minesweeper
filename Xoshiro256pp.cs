namespace Minesweeper;

internal sealed class Xoshiro256pp
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public Xoshiro256pp(UInt128 seed)
    {
        ulong z = seed == UInt128.Zero ? 0x9e3779b97f4a7c15UL : (ulong)seed;
        _s0 = SplitMix64(ref z);
        _s1 = SplitMix64(ref z);
        z += (ulong)(seed >> 64);
        _s2 = SplitMix64(ref z);
        _s3 = SplitMix64(ref z);
    }

    public ulong NextUInt64()
    {
        ulong result = Rotl(_s0 + _s3, 23) + _s0;

        ulong t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;

        _s2 ^= t;
        _s3 = Rotl(_s3, 45);

        return result;
    }

    public ulong NextUInt64(ulong max)
    {
        if (max == 0)
            throw new ArgumentOutOfRangeException(nameof(max));

        // Largest multiple of max that fits in ulong.
        ulong limit = ulong.MaxValue - (ulong.MaxValue % max);

        while (true)
        {
            ulong value = NextUInt64();

            if (value < limit)
                return value % max;
        }
    }

    private static ulong SplitMix64(ref ulong state)
    {
        ulong z = state += 0x9e3779b97f4a7c15UL;
        z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
        z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
        return z ^ (z >> 31);
    }

    private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));
}
