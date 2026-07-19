
namespace Minesweeper;

internal sealed class AdjacentTiles
{
    private const int MaxAdjacentTiles = 8;

    private readonly int[] _indices;
    private readonly byte[] _counts;
    private readonly int _width;

    public AdjacentTiles(int width, int height)
    {
        _width = width;

        int tileCount = width * height;

        _indices = new int[tileCount * MaxAdjacentTiles];
        _counts = new byte[tileCount];

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int tileIndex = x + y * width;
                int offset = tileIndex * MaxAdjacentTiles;
                int count = 0;

                for (int dy = -1; dy <= 1; ++dy)
                {
                    for (int dx = -1; dx <= 1; ++dx)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int adjacentX = x + dx;
                        int adjacentY = y + dy;

                        if (adjacentX < 0 ||
                            adjacentX >= width ||
                            adjacentY < 0 ||
                            adjacentY >= height)
                        {
                            continue;
                        }

                        _indices[offset + count] =
                            adjacentX + adjacentY * width;

                        ++count;
                    }
                }

                _counts[tileIndex] = (byte)count;
            }
        }
    }

    public ReadOnlySpan<int> Get(int tileIndex)
    {
        int offset = tileIndex * MaxAdjacentTiles;

        return _indices.AsSpan(offset, _counts[tileIndex]);
    }

    public ReadOnlySpan<int> Get(int x, int y)
    {
        return Get(x + y * _width);
    }
}