using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Ultralisk : Strategy
    {
        private static Ultralisk Singleton = new Ultralisk();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ULTRALISK) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ULTRALISK_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Ultralisk";
        }
    }
}
