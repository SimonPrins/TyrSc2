using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class MassHydra : Strategy
    {
        private static Strategy Singleton = new MassHydra();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK_BURROWED) >= 10;
        }

        public override string Name()
        {
            return "MassHydra";
        }
    }
}
