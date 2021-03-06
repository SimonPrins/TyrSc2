﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Liberator : Strategy
    {
        private static Liberator Singleton = new Liberator();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.LIBERATOR) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.LIBERATOR_AG) > 0;
        }

        public override string Name()
        {
            return "Liberator";
        }
    }
}
