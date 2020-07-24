using Tyr.Agents;

namespace Tyr.StrategyAnalysis
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
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK_BURROWED) >= 10;
        }

        public override string Name()
        {
            return "MassHydra";
        }
    }
}
