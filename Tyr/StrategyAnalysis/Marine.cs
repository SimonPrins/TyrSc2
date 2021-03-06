﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Marine : Strategy
    {
        private static Marine Singleton = new Marine();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.MARINE) > 0;
        }

        public override string Name()
        {
            return "Marine";
        }
    }
}
