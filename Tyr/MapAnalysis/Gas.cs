using SC2APIProtocol;

namespace Tyr.MapAnalysis
{
    public class Gas
    {
        public Point Pos { get; set; }
        public ulong Tag { get; set; }
        public bool Available { get; set; }
        public bool CanBeGathered { get; set; }
        public Unit Unit { get; set; }
    }
}
