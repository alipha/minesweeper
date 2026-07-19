
namespace Minesweeper;

internal class Board
{
    public Tile[] Tiles;
    public int Width;
    public int Height;
    public int Bombs;
    public ulong Seed;
    public int StartX;
    public int StartY;

    private readonly AdjacentTiles adjacentTiles;
    private readonly Stack<int> revealStack;
    private readonly bool[] revealQueued;

    public Board(int width, int height, int bombs) {
        Tiles = new Tile[width * height];
        Width = width;
        Height = height;
        Bombs = bombs;
        adjacentTiles = new AdjacentTiles(width, height);
        revealStack = new Stack<int>(Tiles.Length);
        revealQueued = new bool[Tiles.Length];
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
        ReadOnlySpan<int> adjacentIndices = adjacentTiles.Get(startIndex);
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
            Tiles[adjacentIndex].IsMine = true;
        }

        // Calculate adjacent mines
        for (int i = 0; i < Tiles.Length; ++i)
        {
            if (Tiles[i].IsMine)
                continue;
            adjacentIndices = adjacentTiles.Get(i);
            int count = 0;
            foreach (int adjacentIndex in adjacentIndices)
            {
                if (Tiles[adjacentIndex].IsMine)
                    ++count;
            }
            Tiles[i].AdjacentMines = count;
        }
    }

    public void Reveal(int index)
    {
        if (Tiles[index].State == TileState.Revealed)
            QueueReveal(index);
        else
            RevealTile(index);

        while (revealStack.Count != 0)
        {
            int currentIndex = revealStack.Pop();
            revealQueued[currentIndex] = false;

            ref Tile currentTile = ref Tiles[currentIndex];
            if (currentTile.State != TileState.Revealed)
                continue;

            if (currentTile.IsMine)
                throw new InvalidOperationException("A mine cannot be revealed.");

            ReadOnlySpan<int> adjacentIndices = adjacentTiles.Get(currentIndex);
            int flaggedCount = 0;
            int hiddenCount = 0;

            foreach (int adjacentIndex in adjacentIndices)
            {
                switch (Tiles[adjacentIndex].State)
                {
                    case TileState.Flagged:
                        ++flaggedCount;
                        break;
                    case TileState.Hidden:
                        ++hiddenCount;
                        break;
                }
            }

            if (hiddenCount == 0)
                continue;

            if (flaggedCount == currentTile.AdjacentMines)
            {
                foreach (int adjacentIndex in adjacentIndices)
                    RevealTile(adjacentIndex);
            }
            else if (flaggedCount + hiddenCount == currentTile.AdjacentMines)
            {
                foreach (int adjacentIndex in adjacentIndices)
                {
                    ref Tile adjacentTile = ref Tiles[adjacentIndex];
                    if (adjacentTile.State != TileState.Hidden)
                        continue;

                    adjacentTile.State = TileState.Flagged;
                    QueueRevealedNeighbors(adjacentIndex);
                }
            }
        }
    }

    private void RevealTile(int index)
    {
        ref Tile tile = ref Tiles[index];
        if (tile.State != TileState.Hidden)
            return;

        tile.State = TileState.Revealed;
        QueueReveal(index);
        QueueRevealedNeighbors(index);
    }

    private void QueueRevealedNeighbors(int index)
    {
        foreach (int adjacentIndex in adjacentTiles.Get(index))
        {
            if (Tiles[adjacentIndex].State == TileState.Revealed)
                QueueReveal(adjacentIndex);
        }
    }

    private void QueueReveal(int index)
    {
        if (revealQueued[index])
            return;

        revealQueued[index] = true;
        revealStack.Push(index);
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
