using SC2APIProtocol;
using SC2Sharp.Agents;

namespace SC2Sharp.MapAnalysis
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
