using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Baneling : Strategy
    {
        private static Baneling Singleton = new Baneling();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BANELING) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BANELING_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Baneling";
        }
    }
}
