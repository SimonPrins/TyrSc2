using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Mothership : Strategy
    {
        private static Mothership Singleton = new Mothership();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MOTHERSHIP) > 0;
        }

        public override string Name()
        {
            return "Mothership";
        }
    }
}
