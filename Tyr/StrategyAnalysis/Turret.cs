using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Turret : Strategy
    {
        private static Turret Singleton = new Turret();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.MISSILE_TURRET) > 0;
        }

        public override string Name()
        {
            return "Turret";
        }
    }
}
