using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Infestor : Strategy
    {
        private static Infestor Singleton = new Infestor();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.INFESTOR) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.INFESTOR_BURROWED) > 0;
        }

        public override string Name()
        {
            return "Infestor";
        }
    }
}
