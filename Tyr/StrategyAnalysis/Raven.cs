using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Raven : Strategy
    {
        private static Raven Singleton = new Raven();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.RAVEN) > 0;
        }

        public override string Name()
        {
            return "Raven";
        }
    }
}
