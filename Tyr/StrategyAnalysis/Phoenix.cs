using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Phoenix : Strategy
    {
        private static Phoenix Singleton = new Phoenix();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.PHOENIX) > 0;
        }

        public override string Name()
        {
            return "Phoenix";
        }
    }
}
