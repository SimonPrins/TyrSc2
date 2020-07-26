using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class WarpPrism : Strategy
    {
        private static WarpPrism Singleton = new WarpPrism();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.WARP_PRISM) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.WARP_PRISM_PHASING) > 0;
        }

        public override string Name()
        {
            return "WarpPrism";
        }
    }
}
