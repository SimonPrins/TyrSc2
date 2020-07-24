﻿using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class MassRoach : Strategy
    {
        private static Strategy Singleton = new MassRoach();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH_BURROWED) >= 10;
        }

        public override string Name()
        {
            return "MassRoach";
        }
    }
}