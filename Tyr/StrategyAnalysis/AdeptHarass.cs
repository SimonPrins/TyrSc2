using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.StrategyAnalysis
{
    public class AdeptHarass : Strategy
    {
        private static AdeptHarass Singleton = new AdeptHarass();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ADEPT) >= 2 && Bot.Main.Frame <= 22.4 * 60 * 4)
            {
                int closeAdepts = 0;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.ADEPT)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Bot.Main.MapAnalyzer.StartLocation) >= 50 * 50)
                        continue;
                    closeAdepts++;
                }
                return closeAdepts >= 2;
            }
            return false;
        }

        public override string Name()
        {
            return "AdeptHarass";
        }
    }
}
