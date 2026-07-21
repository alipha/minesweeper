#pragma warning disable CS0162  // Unreachable code detected

namespace Minesweeper;

internal class Solver
{
    public const bool DEBUG = true;
    public const bool DEBUG_CHECKS = true;

    private readonly Board board;
    private readonly bool[] revealQueued;
    private readonly Stack<int> revealStack;
    private readonly bool[] basicCheckQueued;
    private readonly Stack<int> basicCheckStack;
    private readonly bool[] frontierQueued;
    private readonly Stack<int> frontierStack;
    private readonly int[] reachableGeneration;
    private readonly Stack<int> reachableStack;
    private int currentReachableGeneration;

    public Solver(Board board)
    {
        this.board = board;
        revealQueued = new bool[board.Tiles.Length];
        revealStack = new Stack<int>(board.Tiles.Length);
        basicCheckQueued = new bool[board.Tiles.Length];
        basicCheckStack = new Stack<int>(board.Tiles.Length);
        frontierQueued = new bool[board.Tiles.Length];
        frontierStack = new Stack<int>(board.Tiles.Length);
        reachableGeneration = new int[board.Tiles.Length];
        reachableStack = new Stack<int>(board.Tiles.Length);
    }

    public bool Solve(int startX, int startY)
    {
        int startIndex = board.GetIndex(startX, startY);
        if (!AllReachable(startIndex) || TwoTile5050() || Square5050()) return false;

        Reveal(startIndex);
        return false;
    }

    private void Reveal(int index)
    {
        var tiles = board.Tiles; 
        QueueReveal(index);

        while (basicCheckStack.Count > 0 || revealStack.Count > 0 || frontierStack.Count > 0)
        {
            FloodReveal();

            if (basicCheckStack.Count > 0)
            {
                BasicChecks();
                continue;
            }

            if (frontierStack.Count > 0)
                CheckFrontier();
        }

        if (DEBUG_CHECKS && revealQueued.Any(queued => queued))
            throw new InvalidOperationException("revealQueued should be empty at solve end.");
        if (DEBUG_CHECKS && basicCheckQueued.Any(queued => queued))
            throw new InvalidOperationException("basicCheckQueued should be empty at solve end.");
    }

    private void CheckFrontier()
    {
        var tiles = board.Tiles;
        Span<int> leftHiddenIndices = stackalloc int[3];
        Span<int> rightHiddenIndices = stackalloc int[3];

        while (frontierStack.Count > 0)
        {
            int index = frontierStack.Pop();
            ref Tile tile = ref tiles[index];
            int x = index % board.Width;
            int y = index / board.Width;

            if (DEBUG_CHECKS && !frontierQueued[index])
                throw new InvalidOperationException($"CheckFrontier: Tile {index} is not queued");
            frontierQueued[index] = false;

            if (tile.NeighborsKnown)
            {
                CheckNeighborsKnown("CheckFrontier", index);
                continue;
            }

            RevealedTileDebugChecks("CheckFrontier", index, ref tile);

            if (x + 1 >= board.Width)
                continue;

            int rightIndex = index + 1;
            ref Tile rightTile = ref tiles[rightIndex];
            if (rightTile.State != TileState.Revealed || rightTile.NeighborsKnown)
                continue;
            RevealedTileDebugChecks("CheckFrontier", rightIndex, ref rightTile);

            int sharedHidden = 0;
            int sharedFlagged = 0;
            for (int dy = -1; dy <= 1; dy += 2)
            {
                int adjacentY = y + dy;
                if ((uint)adjacentY >= (uint)board.Height)
                    continue;

                int rowOffset = adjacentY * board.Width;
                for (int dx = 0; dx <= 1; ++dx)
                {
                    switch (tiles[rowOffset + x + dx].State)
                    {
                        case TileState.Hidden:
                            ++sharedHidden;
                            break;
                        case TileState.Flagged:
                            ++sharedFlagged;
                            break;
                    }
                }
            }

            if (sharedHidden == 0)
                continue;

            int leftHidden = 0;
            int rightHidden = 0;
            int leftFlagged = 0;
            int rightFlagged = 0;

            for (int dy = -1; dy <= 1; ++dy)
            {
                int adjacentY = y + dy;
                if ((uint)adjacentY >= (uint)board.Height)
                    continue;

                int rowOffset = adjacentY * board.Width;

                if (x > 0)
                {
                    int leftIndex = rowOffset + x - 1;
                    if (tiles[leftIndex].State == TileState.Hidden)
                        leftHiddenIndices[leftHidden++] = leftIndex;
                    else if (tiles[leftIndex].State == TileState.Flagged)
                        ++leftFlagged;
                }

                if (x + 2 < board.Width)
                {
                    int farRightIndex = rowOffset + x + 2;
                    if (tiles[farRightIndex].State == TileState.Hidden)
                        rightHiddenIndices[rightHidden++] = farRightIndex;
                    else if (tiles[farRightIndex].State == TileState.Flagged)
                        ++rightFlagged;
                }
            }

            int leftRemaining = tile.AdjacentMines - sharedFlagged - leftFlagged;
            int rightRemaining = rightTile.AdjacentMines - sharedFlagged - rightFlagged;

            if (rightRemaining - leftRemaining != rightHidden)
                continue;

            for (int i = 0; i < rightHidden; ++i)
                Flag(rightHiddenIndices[i]);

            for (int i = 0; i < leftHidden; ++i)
                QueueReveal(leftHiddenIndices[i]);

            return;
        }
    }

    private void BasicChecks()
    {
        var tiles = board.Tiles;
        int index = basicCheckStack.Pop();
        ref Tile tile = ref tiles[index];

        if (DEBUG_CHECKS && !basicCheckQueued[index])
            throw new InvalidOperationException($"BasicChecks: Tile {index} is not queued");
        RevealedTileDebugChecks("BasicChecks", index, ref tile);

        ReadOnlySpan<int> adjacentIndices = board.AdjacentTiles.Get(index);
        AdjacentCounts counts = Counts(adjacentIndices);

        if (DEBUG_CHECKS && counts.Flagged > tile.AdjacentMines)
            throw new InvalidOperationException($"Tile {index} has more flags than adjacent mines.");
        if (DEBUG_CHECKS && counts.Flagged + counts.Hidden < tile.AdjacentMines)
            throw new InvalidOperationException($"Tile {index} cannot have enough remaining mines.");

        if (counts.Hidden == 0)
        {
            tile.NeighborsKnown = true;
        }
        else if (counts.Flagged == tile.AdjacentMines)
        {
            foreach (int adjacentIndex in adjacentIndices)
            {
                if (tiles[adjacentIndex].State == TileState.Hidden)
                {
                    QueueReveal(adjacentIndex);
                }
            }
            tile.NeighborsKnown = true;
        }
        else if (counts.Flagged + counts.Hidden == tile.AdjacentMines)
        {
            foreach (int adjacentIndex in adjacentIndices)
            {
                Flag(adjacentIndex);
            }
            tile.NeighborsKnown = true;
        }
        else if (!frontierQueued[index])
        {
            frontierQueued[index] = true;
            frontierStack.Push(index);
        }

        if (tile.NeighborsKnown)
            CheckNeighborsKnown("BasicChecks", index);

        basicCheckQueued[index] = false;
    }

    private void FloodReveal()
    {
        var tiles = board.Tiles;

        while (revealStack.Count > 0)
        {
            int index = revealStack.Pop();
            if (DEBUG_CHECKS && !revealQueued[index])
                throw new InvalidOperationException($"FloodReveal: Tile {index} is not queued");
            revealQueued[index] = false;

            if (DEBUG_CHECKS && tiles[index].State != TileState.Hidden)
                throw new InvalidOperationException($"FloodReveal: Tile {index} is not hidden");
            if (DEBUG_CHECKS && tiles[index].IsMine)
                throw new InvalidOperationException($"FloodReveal: Tile {index} is a mine");

            tiles[index].State = TileState.Revealed;
            if (tiles[index].AdjacentMines == 0)
            {
                ReadOnlySpan<int> adjacentIndices = board.AdjacentTiles.Get(index);
                foreach (int adjacentIndex in adjacentIndices)
                {
                    if (tiles[adjacentIndex].State == TileState.Hidden)
                    {
                        QueueReveal(adjacentIndex);
                    }
                }
            }
            else
            {
                QueueBasicCheck(index);
            }
        }
    }

    private void Flag(int index)
    {
        var tiles = board.Tiles;
        if (tiles[index].State != TileState.Hidden)
            return;

        if (DEBUG_CHECKS && !tiles[index].IsMine)
            throw new InvalidOperationException($"Flag: Tile {index} is not a mine");

        tiles[index].State = TileState.Flagged;
        QueueAdjacentBasicChecks(index);
    }

    private void QueueAdjacentBasicChecks(int index)
    {
        ReadOnlySpan<int> adjacentIndices = board.AdjacentTiles.Get(index);
        foreach (int adjacentIndex in adjacentIndices)
        {
            if (board.Tiles[adjacentIndex].State == TileState.Revealed)
            {
                QueueBasicCheck(adjacentIndex);
            }
        }
    }

    private AdjacentCounts Counts(ReadOnlySpan<int> adjacentIndices)
    {
        int flaggedCount = 0;
        int hiddenCount = 0;
        foreach (int adjacentIndex in adjacentIndices)
        {
            switch (board.Tiles[adjacentIndex].State)
            {
                case TileState.Flagged:
                    ++flaggedCount;
                    break;
                case TileState.Hidden:
                    ++hiddenCount;
                    break;
            }
        }

        return new AdjacentCounts(flaggedCount, hiddenCount);
    }

    private void QueueReveal(int index)
    {
        if (DEBUG_CHECKS && board.Tiles[index].State != TileState.Hidden)
            throw new InvalidOperationException($"QueueReveal: Tile {index} is not hidden");
        if (DEBUG_CHECKS && board.Tiles[index].IsMine)
            throw new InvalidOperationException($"QueueReveal: Tile {index} is a mine");

        if (revealQueued[index])
            return;

        revealQueued[index] = true;
        revealStack.Push(index);
        QueueAdjacentBasicChecks(index);
    }

    private void QueueBasicCheck(int index)
    {
        if (DEBUG_CHECKS && board.Tiles[index].State != TileState.Revealed)
            throw new InvalidOperationException($"QueueBasicCheck: Tile {index} is not revealed");
        if (DEBUG_CHECKS && board.Tiles[index].IsMine)
            throw new InvalidOperationException($"QueueBasicCheck: Tile {index} is a mine");

        if (basicCheckQueued[index])
            return;

        if (board.Tiles[index].NeighborsKnown) {
            CheckNeighborsKnown("QueueBasicCheck", index);
            return;
        }

        basicCheckQueued[index] = true;
        basicCheckStack.Push(index);
    }

    private bool AllReachable(int startIndex)
    {
        if (++currentReachableGeneration == 0)
        {
            Array.Clear(reachableGeneration);
            currentReachableGeneration = 1;
        }

        int reachableCount = 1;
        int mines = 0;
        var tiles = board.Tiles;
        reachableGeneration[startIndex] = currentReachableGeneration;
        reachableStack.Push(startIndex);

        while (reachableStack.Count > 0)
        {
            int index = reachableStack.Pop();

            ReadOnlySpan<int> adjacentIndices = board.AdjacentTiles.Get(index);
            foreach (int adjacentIndex in adjacentIndices)
            {
                if (reachableGeneration[adjacentIndex] == currentReachableGeneration)
                    continue;

                ++reachableCount;
                reachableGeneration[adjacentIndex] = currentReachableGeneration;

                if (tiles[adjacentIndex].IsMine)
                    ++mines;
                else
                    reachableStack.Push(adjacentIndex);
            }
        }

        int remainingTiles = board.Tiles.Length - reachableCount;
        return mines == board.Bombs || mines + remainingTiles == board.Bombs; 
    }

    private bool TwoTile5050()
    {
        var tiles = board.Tiles;
        int width = board.Width;
        int height = board.Height;

        for (int y = 0; y < height; ++y)
        {
            int rowOffset = y * width;

            for (int x = 0; x < width; ++x)
            {
                bool isMine = tiles[rowOffset + x].IsMine;

                if (y + 1 < height &&
                    isMine != tiles[rowOffset + width + x].IsMine &&
                    IsMineOrEdge(x - 1, y - 1) &&
                    IsMineOrEdge(x, y - 1) &&
                    IsMineOrEdge(x + 1, y - 1) &&
                    IsMineOrEdge(x - 1, y + 2) &&
                    IsMineOrEdge(x, y + 2) &&
                    IsMineOrEdge(x + 1, y + 2))
                {
                    return true;
                }

                if (x + 1 < width &&
                    isMine != tiles[rowOffset + x + 1].IsMine &&
                    IsMineOrEdge(x - 1, y - 1) &&
                    IsMineOrEdge(x - 1, y) &&
                    IsMineOrEdge(x - 1, y + 1) &&
                    IsMineOrEdge(x + 2, y - 1) &&
                    IsMineOrEdge(x + 2, y) &&
                    IsMineOrEdge(x + 2, y + 1))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool Square5050()
    {
        var tiles = board.Tiles;
        int width = board.Width;
        int height = board.Height;

        for (int y = 0; y + 1 < height; ++y)
        {
            int topRowOffset = y * width;
            int bottomRowOffset = topRowOffset + width;

            for (int x = 0; x + 1 < width; ++x)
            {
                bool topLeft = tiles[topRowOffset + x].IsMine;
                bool topRight = tiles[topRowOffset + x + 1].IsMine;

                if (topLeft == topRight ||
                    topLeft != tiles[bottomRowOffset + x + 1].IsMine ||
                    topRight != tiles[bottomRowOffset + x].IsMine)
                {
                    continue;
                }

                if (IsMineOrEdge(x - 1, y - 1) &&
                    IsMineOrEdge(x + 2, y - 1) &&
                    IsMineOrEdge(x - 1, y + 2) &&
                    IsMineOrEdge(x + 2, y + 2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsMineOrEdge(int x, int y)
    {
        return x < 0 || x >= board.Width ||
               y < 0 || y >= board.Height ||
               board.Tiles[x + y * board.Width].IsMine;
    }

    private void CheckNeighborsKnown(string func, int index)
    {
        if (!DEBUG_CHECKS)
            return;

        ref Tile tile = ref board.Tiles[index];

        if (tile.State != TileState.Revealed || tile.IsMine)
            throw new InvalidOperationException($"{func}: Tile {index} has invalid NeighborsKnown state.");

        foreach (int adjacentIndex in board.AdjacentTiles.Get(index))
        {
            ref Tile adjacentTile = ref board.Tiles[adjacentIndex];

            if (adjacentTile.State == TileState.Hidden && !revealQueued[adjacentIndex])
                throw new InvalidOperationException($"{func}: Tile {index} has an unknown neighbor {adjacentIndex}.");
        }
    }

    private void RevealedTileDebugChecks(string func, int index, ref Tile tile)
    {
        if (!DEBUG_CHECKS)
            return;
        if (tile.State != TileState.Revealed)
            throw new InvalidOperationException($"{func}: Tile {index} is not revealed");
        if (tile.IsMine)
            throw new InvalidOperationException($"{func}: Tile {index} is a mine");
        if (tile.NeighborsKnown)
            throw new InvalidOperationException($"{func}: Tile {index} is neighbors known");
    }
}
