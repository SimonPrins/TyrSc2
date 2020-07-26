using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.GHOST) > 0;
        }

        public override string Name()
        {
            return "Ghost";
        }
    }
}
