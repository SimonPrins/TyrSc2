using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class FourRax : Strategy
    {
        private static Strategy Singleton = new FourRax();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.Frame <= 22.4 * 60 * 3
                && Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.BARRACKS) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.BARRACKS_FLYING) >= 3;
        }

        public override string Name()
        {
            return "FourRax";
        }
    }
}
