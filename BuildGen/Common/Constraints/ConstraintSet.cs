using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildGen.Constraints
{
    public class ConstraintSet
    {
        private List<ZoneDefinition> zoneDefinitions;

        public List<ZoneDefinition> ZoneDefinitions { get { return zoneDefinitions; } }

        public ConstraintSet()
        {
            zoneDefinitions = new List<ZoneDefinition>();
        }

        public bool RegisterZoneDefinition(string id, ZoneType type, string splitConstraintSet, double width, double height, int minAmount, int maxAmount)
        {
            return RegisterZoneDefinition(id, type, splitConstraintSet, width, width, height, height, minAmount, maxAmount);
        }

        public bool RegisterZoneDefinition(string id, ZoneType type, string splitConstraintSet, double minWidth, double maxWidth, double minHeight, double maxHeight, int minAmount, int maxAmount)
        {
            if ((GetZoneDefinitionById(id) != null) || (minWidth > maxWidth) || (minHeight > maxHeight) || string.IsNullOrEmpty(id))
            {
                return false;
            }
            else
            {
                ZoneDefinition ndef = new ZoneDefinition();
                ndef.Id = id;
                ndef.Type = type;
                ndef.SplitConstraintSet = splitConstraintSet;

                ndef.MinWidth = minWidth;
                ndef.MaxWidth = maxWidth;
                ndef.MinHeight = minHeight;
                ndef.MaxHeight = maxHeight;

                ndef.MinAmount = minAmount;
                ndef.MaxAmount = maxAmount;

                zoneDefinitions.Add(ndef);

                return true;
            }
        }

        public ZoneDefinition GetZoneDefinitionById(string id)
        {
            foreach (var zoneDef in zoneDefinitions)
            {
                if (zoneDef.Id == id)
                    return zoneDef;
            }

            return null;
        }
    }
}
