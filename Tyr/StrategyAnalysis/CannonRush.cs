using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class CannonRush : Strategy
    {
        private static Strategy Singleton = new CannonRush();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (unit.UnitType != UnitTypes.PYLON && unit.UnitType != UnitTypes.PHOTON_CANNON)
                    continue;
                if (SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation)) <= 40 * 40)
                    return true;
            }
            return false;
        }

        public override string Name()
        {
            return "CannonRush";
        }
    }
}
