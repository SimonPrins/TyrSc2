using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Liberator : Strategy
    {
        private static Liberator Singleton = new Liberator();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.LIBERATOR) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.LIBERATOR_AG) > 0;
        }

        public override string Name()
        {
            return "Liberator";
        }
    }
}
