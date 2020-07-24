using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class SwarmHost : Strategy
    {
        private static SwarmHost Singleton = new SwarmHost();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SWARM_HOST) > 0;
        }

        public override string Name()
        {
            return "SwarmHost";
        }
    }
}
