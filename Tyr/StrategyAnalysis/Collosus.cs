using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Collosus : Strategy
    {
        private static Collosus Singleton = new Collosus();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.COLOSUS) > 0;
        }

        public override string Name()
        {
            return "Collosus";
        }
    }
}
