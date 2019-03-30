using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Stalker : Strategy
    {
        private static Stalker Singleton = new Stalker();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.STALKER) > 0;
        }

        public override string Name()
        {
            return "Stalker";
        }
    }
}
