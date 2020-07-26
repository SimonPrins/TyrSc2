using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Overseer : Strategy
    {
        private static Overseer Singleton = new Overseer();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.OVERSEER) > 0;
        }

        public override string Name()
        {
            return "Overseer";
        }
    }
}
