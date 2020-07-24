using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class VoidRay : Strategy
    {
        private static VoidRay Singleton = new VoidRay();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.VOID_RAY) > 0;
        }

        public override string Name()
        {
            return "VoidRay";
        }
    }
}
