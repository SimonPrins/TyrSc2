﻿using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class BroodLord : Strategy
    {
        private static BroodLord Singleton = new BroodLord();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BROOD_LORD) > 0;
        }

        public override string Name()
        {
            return "BroodLord";
        }
    }
}
