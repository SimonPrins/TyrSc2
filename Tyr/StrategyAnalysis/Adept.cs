using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Adept : Strategy
    {
        private static Adept Singleton = new Adept();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ADEPT) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ADEPT_PHASE_SHIFT) > 0;
        }

        public override string Name()
        {
            return "Adept";
        }
    }
}
