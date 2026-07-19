
namespace Minesweeper;

internal class Base32
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRTVWXYZ>";

    public static string Encode(ulong value)
    {
        if (value == 0)
            return "0";

        Span<char> buffer = stackalloc char[13]; // ceil(64 / 5) = 13 characters
        int pos = buffer.Length;

        while (value != 0)
        {
            buffer[--pos] = Alphabet[(int)(value & 31)];
            value >>= 5;
        }
        return new string(buffer[pos..]);
    }

    public static ulong Decode(string value)
    {
        ulong result = 0;

        foreach (char c in value)
        {
            int digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'H' => c - 'A' + 10,
                'J' => 18,
                'K' => 19,
                'M' => 20,
                'N' => 21,
                'P' => 22,
                'Q' => 23,
                'R' => 24,
                'T' => 25,
                'V' => 26,
                'W' => 27,
                'X' => 28,
                'Y' => 29,
                'Z' => 30,
                '>' => 31,
                _ => throw new ArgumentException($"Invalid character '{c}' in Base32 string.", nameof(value)),
            };

            result = (result << 5) | (uint)digit;
        }

        return result;
    }
}
