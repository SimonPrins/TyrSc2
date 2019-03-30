using Tyr.Agents;

namespace Tyr.StrategyAnalysis
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
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ORACLE) > 0;
        }

        public override string Name()
        {
            return "Oracle";
        }
    }
}
