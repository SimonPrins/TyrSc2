using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Medivac : Strategy
    {
        private static Medivac Singleton = new Medivac();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MEDIVAC) > 0;
        }

        public override string Name()
        {
            return "Medivac";
        }
    }
}
