using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Ghost : Strategy
    {
        private static Ghost Singleton = new Ghost();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.GHOST) > 0;
        }

        public override string Name()
        {
            return "Ghost";
        }
    }
}
