using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Changeling : Strategy
    {
        private static Changeling Singleton = new Changeling();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING)
                + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_MARINE)
                + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_MARINE_SHIELD)
                + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_ZEALOT)
                + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_ZERGLING)
                + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_ZERGLING_WINGS) > 0;
        }

        public override string Name()
        {
            return "Changeling";
        }
    }
}
