using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SENTRY) > 0;
        }

        public override string Name()
        {
            return "Sentry";
        }
    }
}
