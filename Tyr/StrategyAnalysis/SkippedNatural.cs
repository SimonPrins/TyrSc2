using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class SkippedNatural : Strategy
    {
        private static SkippedNatural Singleton = new SkippedNatural();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return false;
            if (Bot.Bot.Frame >= 22.4 * 60 * 4.5)
                return false;

            Point2D enemyMain = Bot.Bot.TargetManager.PotentialEnemyStartLocations[0];
            Point2D enemyNatural = Bot.Bot.MapAnalyzer.GetEnemyNatural().Pos;

            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (!UnitTypes.ResourceCenters.Contains(enemy.UnitType))
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, enemyMain) <= 4 * 4)
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, enemyNatural) <= 4 * 4)
                    continue;
                return true;
            }
            return false;
        }

        public override string Name()
        {
            return "SkippedNatural";
        }
    }
}
