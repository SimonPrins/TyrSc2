using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SPORE_CRAWLER) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SPORE_CRAWLER_UPROOTED) > 0;
        }

        public override string Name()
        {
            return "SporeCrawler";
        }
    }
}
