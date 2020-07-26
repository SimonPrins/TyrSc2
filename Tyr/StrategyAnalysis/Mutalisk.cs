using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
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
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.MUTALISK) > 0;
        }

        public override string Name()
        {
            return "Mutalisk";
        }
    }
}
