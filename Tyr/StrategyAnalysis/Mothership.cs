﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Mothership : Strategy
    {
        private static Mothership Singleton = new Mothership();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.MOTHERSHIP) > 0;
        }

        public override string Name()
        {
            return "Mothership";
        }
    }
}
