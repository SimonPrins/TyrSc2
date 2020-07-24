using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class ReaperRush : Strategy
    {
        private static Strategy Singleton = new ReaperRush();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.REAPER) >= 2
                && Bot.Main.Frame <= 22.4 * 60 * 4;
        }

        public override string Name()
        {
            return "ReaperRush";
        }
    }
}
