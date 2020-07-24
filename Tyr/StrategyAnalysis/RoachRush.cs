using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class RoachRush : Strategy
    {
        private static RoachRush Singleton = new RoachRush();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) >= 3 && Bot.Main.Frame <= 22.4 * 60 * 3.5;
        }

        public override string Name()
        {
            return "RoachRush";
        }
    }
}
