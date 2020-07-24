using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Corruptor : Strategy
    {
        private static Corruptor Singleton = new Corruptor();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CORRUPTOR) > 0;
        }

        public override string Name()
        {
            return "Corruptor";
        }
    }
}
