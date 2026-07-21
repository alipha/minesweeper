
namespace Minesweeper;

internal class Board
{
    public readonly Tile[] Tiles;

    public readonly AdjacentTiles AdjacentTiles;
    public readonly int Width;
    public readonly int Height;
    public readonly int Bombs;
    public ulong Seed { get; private set; }
    public int StartX { get; private set; }
    public int StartY { get; private set; }

    public Board(int width, int height, int bombs) {
        if (bombs * 3 > width * height)
            throw new ArgumentException($"Too many bombs: {bombs} > {width * height / 3}");
        Tiles = new Tile[width * height];
        Width = width;
        Height = height;
        Bombs = bombs;
        AdjacentTiles = new AdjacentTiles(width, height);
    }

    public int GetIndex(int x, int y)
    {
        if (Solver.DEBUG_CHECKS && (x < 0 || x >= Width || y < 0 || y >= Height))
            throw new ArgumentOutOfRangeException($"Invalid coordinates: ({x}, {y})");
        return x + y * Width;
    }

    public void Generate(ulong seed, int startX, int startY)
    {
        Seed = seed;
        StartX = startX;
        StartY = startY;
        Xoshiro256pp rng = new Xoshiro256pp(seed);

        for(int i = 0; i < Tiles.Length; ++i)
        {
            Tiles[i] = new Tile();
        }

        int startIndex = startX + startY * Width;
        Tiles[startIndex].IsMine = true;
        ReadOnlySpan<int> adjacentIndices = AdjacentTiles.Get(startIndex);
        foreach (int adjacentIndex in adjacentIndices)
        {
            Tiles[adjacentIndex].IsMine = true;
        }

        // Place bombs
        for (int i = 0; i < Bombs; ++i)
        {
            int index;
            do
            {
                index = (int)rng.NextUInt64((ulong)Tiles.Length);
            } while (Tiles[index].IsMine);
            Tiles[index].IsMine = true;
        }

        Tiles[startIndex].IsMine = false; // Ensure the starting tile is not a mine
        foreach (int adjacentIndex in adjacentIndices)
        {
            Tiles[adjacentIndex].IsMine = false;
        }

        if (Solver.DEBUG_CHECKS && Tiles.Count(tile => tile.IsMine) != Bombs)
            throw new InvalidOperationException("Generated mine count does not match Board.Bombs.");

        // Calculate adjacent mines
        for (int i = 0; i < Tiles.Length; ++i)
        {
            if (Tiles[i].IsMine)
                continue;
            adjacentIndices = AdjacentTiles.Get(i);
            int count = 0;
            foreach (int adjacentIndex in adjacentIndices)
            {
                if (Tiles[adjacentIndex].IsMine)
                    ++count;
            }
            Tiles[i].AdjacentMines = count;
        }
    }

    public string SeedBase32
    {
        get
        {
            return Base32.Encode(Seed);
        }
        set
        {
            Seed = Base32.Decode(value);
        }
    }
}
