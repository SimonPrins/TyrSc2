using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class HighTemplar : Strategy
    {
        private static HighTemplar Singleton = new HighTemplar();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HIGH_TEMPLAR) > 0;
        }

        public override string Name()
        {
            return "HighTemplar";
        }
    }
}
