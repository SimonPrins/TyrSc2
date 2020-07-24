using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Ravager : Strategy
    {
        private static Ravager Singleton = new Ravager();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.RAVAGER) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.RAVAGER_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Ravager";
        }
    }
}
