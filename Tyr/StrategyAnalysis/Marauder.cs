using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Marauder : Strategy
    {
        private static Marauder Singleton = new Marauder();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.MARAUDER) > 0;
        }

        public override string Name()
        {
            return "Marauder";
        }
    }
}
