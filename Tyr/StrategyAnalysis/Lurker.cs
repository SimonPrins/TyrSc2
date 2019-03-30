using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Lurker : Strategy
    {
        private static Lurker Singleton = new Lurker();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.LURKER) + Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.LURKER_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Lurker";
        }
    }
}
