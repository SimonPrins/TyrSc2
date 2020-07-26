using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Carrier : Strategy
    {
        private static Carrier Singleton = new Carrier();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.CARRIER) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.INTERCEPTOR) > 0;
        }

        public override string Name()
        {
            return "Carrier";
        }
    }
}
