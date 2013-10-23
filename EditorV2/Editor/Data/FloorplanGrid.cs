using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildGen.Data
{
    public enum FloorTileType
    {
        Unavailable,    // Not part of the building
        Vacant,         // Empty and ready to be used
        Passage,        // A corridor
        Room,
        Other,          // Entrances
    }

    public class FloorplanGrid
    {
        private FloorTileType[] tiles;
        private int stride;

        public int TileCount { get { return tiles.Count(); } }
        public int Stride { get { return stride; } }
        public int Columns { get { return stride; } }
        public int Rows { get { return TileCount / Columns; } }
        public FloorTileType[] Tiles { get { return tiles; } }

        public FloorTileType this[int i]
        {
            get { return tiles[i]; }
            set { tiles[i] = value; }
        }
        
        public FloorplanGrid(int tileCount, int tileStride)
        {
            if ((tileCount % tileStride) != 0)
                throw new ArgumentException("tileCount is not divisible by tileStride");

            tiles = new FloorTileType[tileCount];
            stride = tileStride;

            Reset();
        }

        public FloorplanGrid Clone()
        {
            FloorplanGrid ngrid = new FloorplanGrid(TileCount, Stride);
            tiles.CopyTo(ngrid.tiles, 0);

            return ngrid;
        }

        public void Reset()
        {
            for (int n = 0; n < TileCount; n++)
            {
                tiles[n] = FloorTileType.Vacant;
            }
        }

        public void Set(int x, int y, int xx, int yy, FloorTileType state)
        {
            for (int nx = x; nx < xx; nx++)
            {
                for (int ny = y; ny < yy; ny++)
                {
                    Set(nx, ny, state);
                }
            }
        }

        public void Set(int x, int y, FloorTileType state)
        {
            int v = ToIndex(x, y);

            if(v != -1)
                tiles[v] = state;
        }

        public bool CheckType(int x, int y, FloorTileType state)
        {
            int n = ToIndex(x, y);

            if (n == -1)
                return false;
            else
                return tiles[n] == state;
        }

        public bool CheckUnavailable(int x, int y)
        {
            int n = ToIndex(x, y);
            return ((n == -1) || (tiles[n] == FloorTileType.Unavailable));
        }

        public bool CheckAreaState(int x, int y, int xx, int yy, FloorTileType type)
        {
            for (int nx = x; nx < xx; nx++)
            {
                for (int ny = y; ny < yy; ny++)
                {
                    if (!CheckType(nx, ny, type))
                        return false;
                }
            }

            return true;
        }

        public List<Rectangle> FindRectangles(FloorTileType type)
        {
            List<Rectangle> ret = new List<Rectangle>();

            int yStart = -1;

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (!CheckType(x, y, type))
                    {
                        if (yStart != -1)
                        {
                            ret.Add(new Rectangle(x, yStart, x + 1, y));
                            yStart = -1;
                        }
                    }
                    else
                    {
                        if (yStart == -1)
                            yStart = y;
                    }
                }

                if (yStart != -1)
                {
                    ret.Add(new Rectangle(x, yStart, x + 1, Rows));
                    yStart = -1;
                }
            }

            return ret;
        }

        public List<Rectangle> FindRectanglesExclusive(FloorTileType typeToExclude)
        {
            List<Rectangle> ret = new List<Rectangle>();

            int yStart = -1;

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (CheckType(x, y, typeToExclude))
                    {
                        if (yStart != -1)
                        {
                            ret.Add(new Rectangle(x, yStart, x + 1, y));
                            yStart = -1;
                        }
                    }
                    else
                    {
                        if (yStart == -1)
                            yStart = y;
                    }
                }

                if (yStart != -1)
                {
                    ret.Add(new Rectangle(x, yStart, x + 1, Rows));
                    yStart = -1;
                }
            }

            return ret;
        }

        public int ToIndex(int x, int y)
        {
            if ((x >= Columns) || (x < 0) ||
                (y >= Rows) || (y < 0))
                return -1;

            return x + y * Stride;
        }

        public Point ToPosition(int index)
        {
            if ((index < 0) || (index >= TileCount))
                throw new ArgumentOutOfRangeException();

            Point ret = new Point();
            ret.X = index % Columns;
            ret.Y = (index - (index % Columns)) / Columns;

            return ret;
        }
    }
}
