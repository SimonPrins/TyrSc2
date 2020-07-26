using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Corruptor : Strategy
    {
        private static Corruptor Singleton = new Corruptor();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CORRUPTOR) > 0;
        }

        public override string Name()
        {
            return "Corruptor";
        }
    }
}
