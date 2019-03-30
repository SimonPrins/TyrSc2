using Tyr.Agents;

namespace Tyr.StrategyAnalysis
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
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.WARP_PRISM) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.WARP_PRISM_PHASING) > 0;
        }

        public override string Name()
        {
            return "WarpPrism";
        }
    }
}
