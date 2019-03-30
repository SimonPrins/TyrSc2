using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Mutalisk : Strategy
    {
        private static Mutalisk Singleton = new Mutalisk();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MUTALISK) > 0;
        }

        public override string Name()
        {
            return "Mutalisk";
        }
    }
}
