using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Marine : Strategy
    {
        private static Marine Singleton = new Marine();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MARINE) > 0;
        }

        public override string Name()
        {
            return "Marine";
        }
    }
}
