using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.MISSILE_TURRET) > 0;
        }

        public override string Name()
        {
            return "Turret";
        }
    }
}
