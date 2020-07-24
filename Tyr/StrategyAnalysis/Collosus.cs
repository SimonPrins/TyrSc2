using Tyr.Agents;

namespace Tyr.StrategyAnalysis
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
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.COLOSUS) > 0;
        }

        public override string Name()
        {
            return "Collosus";
        }
    }
}
