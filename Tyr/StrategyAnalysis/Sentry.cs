using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Sentry : Strategy
    {
        private static Sentry Singleton = new Sentry();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SENTRY) > 0;
        }

        public override string Name()
        {
            return "Sentry";
        }
    }
}
