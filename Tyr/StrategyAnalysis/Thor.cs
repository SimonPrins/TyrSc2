using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Thor : Strategy
    {
        private static Thor Singleton = new Thor();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.THOR) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.THOR_SINGLE_TARGET) > 0;
        }

        public override string Name()
        {
            return "Thor";
        }
    }
}
