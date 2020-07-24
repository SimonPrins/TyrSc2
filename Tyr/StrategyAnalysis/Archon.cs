using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Archon : Strategy
    {
        private static Archon Singleton = new Archon();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ARCHON) > 0;
        }

        public override string Name()
        {
            return "Archon";
        }
    }
}
