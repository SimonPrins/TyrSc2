using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Cyclone : Strategy
    {
        private static Cyclone Singleton = new Cyclone();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CYCLONE) > 0;
        }

        public override string Name()
        {
            return "Cyclone";
        }
    }
}
