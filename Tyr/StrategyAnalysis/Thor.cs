using Tyr.Agents;

namespace Tyr.StrategyAnalysis
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
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.THOR) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.THOR_SINGLE_TARGET) > 0;
        }

        public override string Name()
        {
            return "Thor";
        }
    }
}
