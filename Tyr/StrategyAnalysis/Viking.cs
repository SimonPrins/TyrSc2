using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Viking : Strategy
    {
        private static Viking Singleton = new Viking();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.VIKING_FIGHTER) + Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.VIKING_ASSUALT) > 0;
        }

        public override string Name()
        {
            return "Viking";
        }
    }
}
