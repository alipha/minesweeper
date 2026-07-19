
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Minesweeper;

internal class Generator
{
    public Board Board;

    public Generator(int width, int height, int bombs, int startX, int startY)
    {
        Board = new Board(width, height, bombs);
        Solver solver = new Solver();

        Xoshiro256pp rng = new Xoshiro256pp(GetRandomUInt128());
        do
        {
            Board.Generate(rng.NextUInt64(), startX, startY);
        } while (!solver.Solve(Board, startX, startY));
    }

    static UInt128 GetRandomUInt128()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        ulong low = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        ulong high = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..]);

        return ((UInt128)high << 64) | low;
    }
}
