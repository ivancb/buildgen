using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildGen.Data
{
    public class Room
    {
        public Point TopLeft;
        public Point BottomRight;
        public Point Entrance;

        public Room()
        {
            TopLeft = new Point();
            BottomRight = new Point();
            Entrance = new Point();
        }
    }
}
