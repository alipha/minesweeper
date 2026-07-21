
namespace Minesweeper;

internal enum TileState
{
    Hidden,
    Revealed,
    Flagged
}

internal struct Tile
{
    public bool IsMine;
    public bool NeighborsKnown;
    public TileState State;
    public int AdjacentMines;

    public Tile()
    {
        IsMine = false;
        NeighborsKnown = false;
        State = TileState.Hidden;
        AdjacentMines = 0;
    }
}
