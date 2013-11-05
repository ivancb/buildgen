
namespace BuildGen.Data
{
    public enum EntranceType
    {
        Terminal, // No path upwards
        Entrance, // No path downwards
        Passage, // Combination of Terminal and Entrance, merely indicates an entrance into another section of the building
        Transition,
    }

    public class Entrance
    {
        public Point GridPosition;
        public EntranceType Type;
        public Direction Direction;

        public Entrance(int x, int y, EntranceType type, Direction direction)
        {
            GridPosition = new Point(x, y);
            Type = type;
            Direction = direction;
        }

        public Point GetConnectionPoint()
        {
            return GridPosition.Advance(Direction, 1);
        }
    }
}
