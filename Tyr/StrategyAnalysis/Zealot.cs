﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Zealot : Strategy
    {
        private static Zealot Singleton = new Zealot();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ZEALOT) > 0;
        }

        public override string Name()
        {
            return "Zealot";
        }
    }
}
