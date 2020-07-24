using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class ZerglingRush : Strategy
    {
        private static ZerglingRush Singleton = new ZerglingRush();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 5 && Bot.Bot.Frame <= 22.4 * 60 * 2.5;
        }

        public override string Name()
        {
            return "ZerglingRush";
        }
    }
}
