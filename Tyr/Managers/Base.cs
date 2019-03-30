using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.MapAnalysis;

namespace Tyr.Managers
{
    public class Base
    {
        public BaseLocation BaseLocation { get; set; }
        public Agent ResourceCenter { get; set; }
        public int ResourceCenterFinishedFrame = -1;
        public int Owner { get; set; }
        public Dictionary<uint, int> BuildingCounts = new Dictionary<uint, int>();
        public Dictionary<uint, int> BuildingsCompleted = new Dictionary<uint, int>();
        public int DistanceToMain { get; set; }
        public bool UnderAttack;
        public bool Blocked;
        public Point2D MineralLinePos;
        public Point2D OppositeMineralLinePos;
        public Point2D MineralSide1;
        public Point2D MineralSide2;
    }
}
