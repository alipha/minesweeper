
namespace Minesweeper;

internal enum TileState
{
    Hidden,
    Revealed,
    Flagged
}

internal struct Tile
{
    public bool IsMine { get; set; }
    public TileState State { get; set; }
    public int AdjacentMines { get; set; }

    public Tile()
    {
        IsMine = false;
        State = TileState.Hidden;
        AdjacentMines = 0;
    }
}
