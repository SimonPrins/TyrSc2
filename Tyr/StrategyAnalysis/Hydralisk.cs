using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Hydralisk : Strategy
    {
        private static Hydralisk Singleton = new Hydralisk();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Hydralisk";
        }
    }
}
