using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Tempest : Strategy
    {
        private static Tempest Singleton = new Tempest();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.TEMPEST) > 0;
        }

        public override string Name()
        {
            return "Tempest";
        }
    }
}
