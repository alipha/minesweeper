namespace Minesweeper;

internal struct AdjacentCounts
{
    public int Flagged;
    public int Hidden;

    public AdjacentCounts(int flagged, int hidden)
    {
        Flagged = flagged;
        Hidden = hidden;
    }
}
