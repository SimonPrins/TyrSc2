using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Reaper : Strategy
    {
        private static Reaper Singleton = new Reaper();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.REAPER) > 0;
        }

        public override string Name()
        {
            return "Reaper";
        }
    }
}
