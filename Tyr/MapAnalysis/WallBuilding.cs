using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.MapAnalysis
{
    public class WallBuilding
    {
        public uint Type;
        public Point2D Pos;

        public Point2D Size {
            get
            {
                return BuildingType.LookUp[Type].Size;
            }
            private set { }
        }
    }
}
