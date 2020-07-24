using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class DarkTemplar : Strategy
    {
        private static DarkTemplar Singleton = new DarkTemplar();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.DARK_TEMPLAR) > 0;
        }

        public override string Name()
        {
            return "DarkTemplar";
        }
    }
}
