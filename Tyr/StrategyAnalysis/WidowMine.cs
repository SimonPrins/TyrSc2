using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class WidowMine : Strategy
    {
        private static WidowMine Singleton = new WidowMine();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.WIDOW_MINE) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.WIDOW_MINE_BURROWED) > 0;
        }

        public override string Name()
        {
            return "WidowMine";
        }
    }
}
