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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.PHOENIX) > 0;
        }

        public override string Name()
        {
            return "Phoenix";
        }
    }
}
