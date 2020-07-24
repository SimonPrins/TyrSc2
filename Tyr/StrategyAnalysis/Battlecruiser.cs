using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Battlecruiser : Strategy
    {
        private static Battlecruiser Singleton = new Battlecruiser();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.BATTLECRUISER) > 0;
        }

        public override string Name()
        {
            return "Battlecruiser";
        }
    }
}
