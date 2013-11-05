using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildGen.Constraints
{
    public class ZoneDefinition
    {
        public string Id = null;
        public string SplitConstraintSet = null;
        public ZoneType Type = ZoneType.Public;

        public double MinWidth = 1.0;
        public double MaxWidth = 1.0;
        public double MinHeight = 1.0;
        public double MaxHeight = 1.0;

        public int MinAmount = 1;
        public int MaxAmount = 1;

        public List<int> ExcludedFloorIndices = new List<int>();
    }
}
