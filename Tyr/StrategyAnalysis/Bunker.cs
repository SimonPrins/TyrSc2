﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Bunker : Strategy
    {
        private static Bunker Singleton = new Bunker();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BUNKER) > 0;
        }

        public override string Name()
        {
            return "Bunker";
        }
    }
}
