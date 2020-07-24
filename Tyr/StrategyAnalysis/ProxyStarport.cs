using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class ProxyStarport : Strategy
    {
        private static ProxyStarport Singleton = new ProxyStarport();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return false;
            if (Bot.Bot.Frame >= 22.4 * 60 * 3)
                return false;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.STARPORT)
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]) >= 40 * 40)
                    return true;
            }
            return false;
        }

        public override string Name()
        {
            return "ProxyStarport";
        }
    }
}
