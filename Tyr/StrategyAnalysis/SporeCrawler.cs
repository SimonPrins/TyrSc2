﻿using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class SporeCrawler : Strategy
    {
        private static SporeCrawler Singleton = new SporeCrawler();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SPORE_CRAWLER) + Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SPORE_CRAWLER_UPROOTED) > 0;
        }

        public override string Name()
        {
            return "SporeCrawler";
        }
    }
}
