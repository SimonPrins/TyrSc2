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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.RAVAGER) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.RAVAGER_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Ravager";
        }
    }
}
