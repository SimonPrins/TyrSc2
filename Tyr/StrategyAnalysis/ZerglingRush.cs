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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 5 && Bot.Main.Frame <= 22.4 * 60 * 2.5;
        }

        public override string Name()
        {
            return "ZerglingRush";
        }
    }
}
