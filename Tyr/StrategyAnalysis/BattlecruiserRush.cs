using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.StrategyAnalysis
{
    public class BattlecruiserRush : Strategy
    {
        private static BattlecruiserRush Singleton = new BattlecruiserRush();
        private bool ApproachingBC = false;

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Detected)
                return true;
            if (ApproachingBC)
                return false;
            if (Bot.Main.Frame > 22.4 * 60 * 6.5)
                return false;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BATTLECRUISER)
                    continue;
                float dist = SC2Util.DistanceSq(enemy.Pos, Bot.Main.MapAnalyzer.StartLocation);
                if (dist < 8 * 8)
                    return true;
                if (dist < 60 * 60)
                {
                    ApproachingBC = true;
                    return false;
                }
            }
            return false;
        }

        public override string Name()
        {
            return "BattlecruiserRush";
        }
    }
}
