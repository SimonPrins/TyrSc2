using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Banshee : Strategy
    {
        private static Banshee Singleton = new Banshee();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.BANSHEE) > 0;
        }

        public override string Name()
        {
            return "Banshee";
        }
    }
}
