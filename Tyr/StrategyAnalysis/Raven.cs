﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Raven : Strategy
    {
        private static Raven Singleton = new Raven();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.RAVEN) > 0;
        }

        public override string Name()
        {
            return "Raven";
        }
    }
}
