using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class ProxyDetected : Strategy
    {
        private static ProxyDetected Singleton = new ProxyDetected();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return false;
            if (Tyr.Bot.Frame >= 22.4 * 60 * 3)
                return false;
            if (Tyr.Bot.EnemyStrategyAnalyzer.Expanded)
                return false;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (!UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, Tyr.Bot.TargetManager.PotentialEnemyStartLocations[0]) >= 40 * 40)
                    return true;
            }
            return false;
        }

        public override string Name()
        {
            return "ProxyDetected";
        }
    }
}
