using System;

namespace BuildGen.Data
{
    public struct Point
    {
        public Point(int x, int y) { X = x; Y = y; }

        public int X;
        public int Y;

        public override bool Equals(object obj)
        {
            return (obj is Point) && (this == (Point)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Point lhs, Point rhs)
        {
            return (lhs.X == rhs.X) && (lhs.Y == rhs.Y);
        }

        public static bool operator !=(Point lhs, Point rhs)
        {
            return !(lhs == rhs);
        }

        public Point Advance(Direction direction, int amount)
        {
            Point ret = this;

            switch (direction)
            {
                case Direction.North:
                    ret.Y -= amount;
                    break;
                case Direction.South:
                    ret.Y += amount;
                    break;
                case Direction.West:
                    ret.X -= amount;
                    break;
                case Direction.East:
                    ret.X += amount;
                    break;
            }

            return ret;
        }

        public Direction GetRelativeDirection(Point v)
        {
            if (v.X < X)
                return Direction.West;
            if (v.X > X)
                return Direction.East;
            if (v.Y < Y)
                return Direction.South;
            if (v.Y > Y)
                return Direction.North;

            return Direction.Unspecified;
        }

        public double Distance(Point v)
        { 
            return Math.Sqrt(((v.X - X) * (v.X - X) + (v.Y - Y) * (v.Y - Y))); 
        }

        public override string ToString()
        {
            return "X=" + X + " Y=" + Y;
        }
    }

    public struct PointF
    {
        public PointF(float x, float y) { X = x; Y = y; }

        public float X;
        public float Y;

        public override bool Equals(object obj)
        {
            return (obj is PointF) && (this == (PointF)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(PointF lhs, PointF rhs)
        {
            return (lhs.X == rhs.X) && (lhs.Y == rhs.Y);
        }

        public static bool operator !=(PointF lhs, PointF rhs)
        {
            return !(lhs == rhs);
        }

        public PointF Advance(Direction direction, float amount)
        {
            PointF ret = this;

            switch (direction)
            {
                case Direction.North:
                    ret.Y -= amount;
                    break;
                case Direction.South:
                    ret.Y += amount;
                    break;
                case Direction.West:
                    ret.X -= amount;
                    break;
                case Direction.East:
                    ret.X += amount;
                    break;
            }

            return ret;
        }

        public double Distance(Point v)
        {
            return Math.Sqrt(((v.X - X) * (v.X - X) + (v.Y - Y) * (v.Y - Y)));
        }

        public override string ToString()
        {
            return "X=" + X + " Y=" + Y;
        }
    }
}
