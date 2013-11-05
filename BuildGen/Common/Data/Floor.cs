using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildGen.Data
{
    public class Floor
    {
        public List<Room> Rooms;
        public List<Entrance> Entrances;
        public FloorplanGrid Grid;

        public Floor(int tilecount, int tileStride)
        {
            Grid = new FloorplanGrid(tilecount, tileStride);
            Entrances = new List<Entrance>();
            Rooms = new List<Room>();
        }

        public Room AddRoom(int x, int y, int xx, int yy)
        {
            Room nroom = new Room();
            nroom.TopLeft = new Point(x, y);
            nroom.BottomRight = new Point(xx, yy);

            Rooms.Add(nroom);

            return nroom;
        }

        public void AddEntrance(int x, int y, EntranceType type, Direction direction)
        {
            Entrance nent = new Entrance(x, y, type, direction);
            Grid.Set(x, y, FloorTileType.Other);

            Entrances.Add(nent);
        }

        public void RemoveEntranceAt(int x, int y)
        {
            for (int n = 0; n < Entrances.Count; n++)
            {
                if ((Entrances[n].GridPosition.X == x) && (Entrances[n].GridPosition.Y == y))
                {
                    Entrances.RemoveAt(n);
                    break;
                }
            }
        }

        public Entrance GetEntranceAt(int x, int y)
        {
            foreach (var entrance in Entrances)
            {
                if ((entrance.GridPosition.X == x) && (entrance.GridPosition.Y == y))
                {
                    return entrance;
                }
            }

            return null;
        }

        public bool Valid
        {
            get
            {
                return Entrances.Count() > 0;
            }
        }
    }
}
