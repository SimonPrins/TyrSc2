using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Oracle : Strategy
    {
        private static Oracle Singleton = new Oracle();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ORACLE) > 0;
        }

        public override string Name()
        {
            return "Oracle";
        }
    }
}
