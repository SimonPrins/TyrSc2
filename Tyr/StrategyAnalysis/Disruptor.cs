﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Disruptor : Strategy
    {
        private static Disruptor Singleton = new Disruptor();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.DISRUPTOR) > 0;
        }

        public override string Name()
        {
            return "Disruptor";
        }
    }
}
