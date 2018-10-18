using System.Collections.Generic;
using SC2APIProtocol;

namespace Tyr.MapAnalysis
{
    public class BaseLocation
    {
        public List<MineralField> MineralFields { get; internal set; } = new List<MineralField>();
        public List<Gas> Gasses { get; internal set; } = new List<Gas>();
        public Point2D Pos { get; set; }
    }
}
