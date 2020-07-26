using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SWARM_HOST) > 0;
        }

        public override string Name()
        {
            return "SwarmHost";
        }
    }
}
