using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Hellbat : Strategy
    {
        private static Hellbat Singleton = new Hellbat();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.HELLBAT) > 0;
        }

        public override string Name()
        {
            return "Hellbat";
        }
    }
}
