using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK_SIEGED) > 0
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.MEDIVAC) > 0
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BANSHEE) > 0
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.THOR) > 0
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HELLION) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HELLBAT) >= 2
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.GHOST) > 0
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.MARAUDER) >= 3
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.LIBERATOR) > 0
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.WIDOW_MINE) >= 2
                    || Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CYCLONE) > 0;
        }

        public override string Name()
        {
            return "TerranTech";
        }
    }
}
