using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Mech : Strategy
    {
        private static Strategy Singleton = new Mech();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.THOR) > 0
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HELLION) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HELLBAT) >= 5
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CYCLONE) > 2;
        }

        public override string Name()
        {
            return "Mech";
        }
    }
}
