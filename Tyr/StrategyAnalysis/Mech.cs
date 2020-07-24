using Tyr.Agents;

namespace Tyr.StrategyAnalysis
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
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.THOR) > 0
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.HELLION) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.HELLBAT) >= 5
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CYCLONE) > 2;
        }

        public override string Name()
        {
            return "Mech";
        }
    }
}
