using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Zealot : Strategy
    {
        private static Zealot Singleton = new Zealot();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ZEALOT) > 0;
        }

        public override string Name()
        {
            return "Zealot";
        }
    }
}
