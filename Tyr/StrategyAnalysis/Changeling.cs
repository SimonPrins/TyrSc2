using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING)
                + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_MARINE)
                + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_MARINE_SHIELD)
                + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_ZEALOT)
                + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_ZERGLING)
                + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CHANGELING_ZERGLING_WINGS) > 0;
        }

        public override string Name()
        {
            return "Changeling";
        }
    }
}
