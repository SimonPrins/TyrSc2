﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class SiegeTank : Strategy
    {
        private static SiegeTank Singleton = new SiegeTank();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK_SIEGED) > 0;
        }

        public override string Name()
        {
            return "SiegeTank";
        }
    }
}
