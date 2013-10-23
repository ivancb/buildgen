using System;
using System.Collections.Generic;

namespace BuildGen.Data
{
    public class Building
    {
        public List<Floor> Floors;
        public int Seed;
        public float Width;
        public float Height;
        public float Resolution;
        public string ConstraintSet;

        public Building(float width, float height, float resolution)
        {
            Width = width;
            Height = height;
            Resolution = resolution;
            Floors = new List<Floor>();
            Seed = 0;
            ConstraintSet = null;
        }

        public Floor AddFloor()
        {
            int rows = (int)Math.Ceiling(Width / Resolution);
            int columns = (int)Math.Ceiling(Height / Resolution);

            Floor nfloor = new Floor(rows * columns, columns);
            Floors.Add(nfloor);

            return nfloor;
        }

        public bool Valid
        {
            get
            {
                foreach (var floor in Floors)
                {
                    if (!floor.Valid)
                        return false;
                }

                return true;
            }
        }
    }
}
