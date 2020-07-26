using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class FourRax : Strategy
    {
        private static Strategy Singleton = new FourRax();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.Frame <= 22.4 * 60 * 3
                && Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BARRACKS) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BARRACKS_FLYING) >= 3;
        }

        public override string Name()
        {
            return "FourRax";
        }
    }
}
