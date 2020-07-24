using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Bunker : Strategy
    {
        private static Bunker Singleton = new Bunker();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.BUNKER) > 0;
        }

        public override string Name()
        {
            return "Bunker";
        }
    }
}
