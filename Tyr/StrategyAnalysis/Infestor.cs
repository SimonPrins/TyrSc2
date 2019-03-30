using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Infestor : Strategy
    {
        private static Infestor Singleton = new Infestor();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.INFESTOR) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.INFESTOR_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Infestor";
        }
    }
}
