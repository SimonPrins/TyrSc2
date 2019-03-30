using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class BroodLord : Strategy
    {
        private static BroodLord Singleton = new BroodLord();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.BROOD_LORD) > 0;
        }

        public override string Name()
        {
            return "BroodLord";
        }
    }
}
