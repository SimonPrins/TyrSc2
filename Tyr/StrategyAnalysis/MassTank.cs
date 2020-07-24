using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class MassTank : Strategy
    {
        private static MassTank Singleton = new MassTank();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SIEGE_TANK_SIEGED) >= 8;
        }

        public override string Name()
        {
            return "MassTank";
        }
    }
}
