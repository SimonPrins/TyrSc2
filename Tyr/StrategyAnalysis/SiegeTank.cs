﻿using Tyr.Agents;

namespace Tyr.StrategyAnalysis
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
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK_SIEGED) > 0;
        }

        public override string Name()
        {
            return "SiegeTank";
        }
    }
}