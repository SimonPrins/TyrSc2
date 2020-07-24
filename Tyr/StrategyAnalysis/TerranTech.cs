using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class TerranTech : Strategy
    {
        private static Strategy Singleton = new TerranTech();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK_SIEGED) > 0
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MEDIVAC) > 0
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.BANSHEE) > 0
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.THOR) > 0
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.HELLION) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.HELLBAT) >= 2
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.GHOST) > 0
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MARAUDER) >= 3
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.LIBERATOR) > 0
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.WIDOW_MINE) >= 2
                    || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.CYCLONE) > 0;
        }

        public override string Name()
        {
            return "TerranTech";
        }
    }
}
