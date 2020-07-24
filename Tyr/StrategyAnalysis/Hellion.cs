using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Hellion : Strategy
    {
        private static Hellion Singleton = new Hellion();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.HELLION) > 0;
        }

        public override string Name()
        {
            return "Hellion";
        }
    }
}
