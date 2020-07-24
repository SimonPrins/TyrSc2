﻿using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class BattlecruiserRush : Strategy
    {
        private static BattlecruiserRush Singleton = new BattlecruiserRush();
        private bool ApproachingBC = false;

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Detected)
                return true;
            if (ApproachingBC)
                return false;
            if (Bot.Bot.Frame > 22.4 * 60 * 6.5)
                return false;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BATTLECRUISER)
                    continue;
                float dist = SC2Util.DistanceSq(enemy.Pos, Bot.Bot.MapAnalyzer.StartLocation);
                if (dist < 8 * 8)
                    return true;
                if (dist < 60 * 60)
                {
                    ApproachingBC = true;
                    return false;
                }
            }
            return false;
        }

        public override string Name()
        {
            return "BattlecruiserRush";
        }
    }
}
