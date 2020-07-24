using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Cyclone : Strategy
    {
        private static Cyclone Singleton = new Cyclone();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CYCLONE) > 0;
        }

        public override string Name()
        {
            return "Cyclone";
        }
    }
}
