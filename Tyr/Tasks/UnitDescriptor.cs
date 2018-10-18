using SC2APIProtocol;
using System.Collections.Generic;

namespace Tyr.Tasks
{
    public class UnitDescriptor
    {
        public HashSet<uint> UnitTypes;
        public Point2D Pos;
        public float MaxDist = 1000000;
        public int Count = -1;
        public object Marker;

        public void AddType(uint type)
        {
            if (UnitTypes == null)
                UnitTypes = new HashSet<uint>();
            UnitTypes.Add(type);
        }
    }
}
