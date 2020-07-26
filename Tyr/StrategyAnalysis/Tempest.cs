using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Tempest : Strategy
    {
        private static Tempest Singleton = new Tempest();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.TEMPEST) > 0;
        }

        public override string Name()
        {
            return "Tempest";
        }
    }
}
