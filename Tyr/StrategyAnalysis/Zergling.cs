using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Zergling : Strategy
    {
        private static Zergling Singleton = new Zergling();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) > 0;
        }

        public override string Name()
        {
            return "Zergling";
        }
    }
}
