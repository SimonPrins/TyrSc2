using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Viper : Strategy
    {
        private static Viper Singleton = new Viper();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.VIPER) > 0;
        }

        public override string Name()
        {
            return "Viper";
        }
    }
}
