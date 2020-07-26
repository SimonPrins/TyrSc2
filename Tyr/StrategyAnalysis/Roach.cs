using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Roach : Strategy
    {
        private static Roach Singleton = new Roach();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Roach";
        }
    }
}
