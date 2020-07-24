using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class CompletedCannonProxy : Strategy
    {
        private static CompletedCannonProxy Singleton = new CompletedCannonProxy();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) == 0)
                return false;
            foreach (Unit enemy in Bot.Bot.Enemies())
                if (enemy.UnitType == UnitTypes.PHOTON_CANNON
                    && enemy.BuildProgress >= 0.9
                    && SC2Util.DistanceSq(enemy.Pos, Bot.Bot.MapAnalyzer.StartLocation) <= 50 * 50)
                    return true;
            return false;
        }

        public override string Name()
        {
            return "CompletedCannonProxy";
        }
    }
}
