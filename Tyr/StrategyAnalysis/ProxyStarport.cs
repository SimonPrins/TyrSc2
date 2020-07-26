using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.StrategyAnalysis
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
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return false;
            if (Bot.Main.Frame >= 22.4 * 60 * 3)
                return false;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.STARPORT)
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, Bot.Main.TargetManager.PotentialEnemyStartLocations[0]) >= 40 * 40)
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
