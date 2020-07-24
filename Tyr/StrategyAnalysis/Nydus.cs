using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Nydus : Strategy
    {
        private static Nydus Singleton = new Nydus();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.NYDUS_CANAL) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.NYDUS_NETWORK) > 0;
        }

        public override string Name()
        {
            return "Nydus";
        }
    }
}
