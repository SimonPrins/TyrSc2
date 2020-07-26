using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class Battlecruiser : Strategy
    {
        private static Battlecruiser Singleton = new Battlecruiser();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.BATTLECRUISER) > 0;
        }

        public override string Name()
        {
            return "Battlecruiser";
        }
    }
}
