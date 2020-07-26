using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ADEPT) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ADEPT_PHASE_SHIFT) > 0;
        }

        public override string Name()
        {
            return "Adept";
        }
    }
}
