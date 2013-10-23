
using System;
namespace BuildGen.Data
{
    public enum Direction
    {
        West,
        East,
        North,
        South,

        Unspecified,
    }

    public static class DirectionExtensions
    {
        public static Direction GetOpposite(this Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                    return Direction.South;
                case Direction.South:
                    return Direction.North;
                case Direction.West:
                    return Direction.East;
                case Direction.East:
                    return Direction.West;
                default:
                    return Direction.Unspecified;
            }
        }

        public static bool IsHorizontal(this Direction dir)
        {
            return (dir == Direction.West) || (dir == Direction.East);
        }

        public static bool IsVertical(this Direction dir)
        {
            return (dir == Direction.North) || (dir == Direction.South);
        }

        public static Direction Advance(this Direction dir, bool reverse)
        {
            switch (dir)
            {
                case Direction.North:
                    if(!reverse)
                        return Direction.South;
                    else
                        return Direction.East;
                case Direction.South:
                    if (!reverse)
                        return Direction.West;
                    else
                        return Direction.North;
                case Direction.West:
                    if (!reverse)
                        return Direction.East;
                    else
                        return Direction.South;
                case Direction.East:
                    if (!reverse)
                        return Direction.North;
                    else
                        return Direction.West;
                default:
                    return Direction.Unspecified;
            }
        }

        public static Direction ToDirection(this int v)
        {
            switch (v)
            {
                case 0:
                    return Direction.North;
                case 1:
                    return Direction.South;
                case 2:
                    return Direction.West;
                case 3:
                    return Direction.East;
                default:
                    return Direction.Unspecified;
            }
        }

        public static string ToString(this Direction v)
        {
            return Enum.GetName(typeof(Direction), v);
        }
    }
}
