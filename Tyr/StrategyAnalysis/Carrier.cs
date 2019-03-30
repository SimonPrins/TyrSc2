using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Carrier : Strategy
    {
        private static Carrier Singleton = new Carrier();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CARRIER) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.INTERCEPTOR) > 0;
        }

        public override string Name()
        {
            return "Carrier";
        }
    }
}
