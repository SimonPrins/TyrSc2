using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Lurker : Strategy
    {
        private static Lurker Singleton = new Lurker();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.LURKER) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.LURKER_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Lurker";
        }
    }
}
