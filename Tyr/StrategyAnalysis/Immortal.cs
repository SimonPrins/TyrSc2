using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Immortal : Strategy
    {
        private static Immortal Singleton = new Immortal();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.IMMORTAL) > 0;
        }

        public override string Name()
        {
            return "Immortal";
        }
    }
}
