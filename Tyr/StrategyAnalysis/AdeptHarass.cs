using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
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
            if (Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ADEPT) >= 2 && Bot.Bot.Frame <= 22.4 * 60 * 4)
            {
                int closeAdepts = 0;
                foreach (Unit enemy in Bot.Bot.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.ADEPT)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Bot.Bot.MapAnalyzer.StartLocation) >= 50 * 50)
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
