using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Marauder : Strategy
    {
        private static Marauder Singleton = new Marauder();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MARAUDER) > 0;
        }

        public override string Name()
        {
            return "Marauder";
        }
    }
}
