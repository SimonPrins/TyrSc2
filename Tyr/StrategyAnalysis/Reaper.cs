﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Reaper : Strategy
    {
        private static Reaper Singleton = new Reaper();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.REAPER) > 0;
        }

        public override string Name()
        {
            return "Reaper";
        }
    }
}
