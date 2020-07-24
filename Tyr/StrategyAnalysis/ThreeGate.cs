using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class ThreeGate : Strategy
    {
        private static Strategy Singleton = new ThreeGate();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Bot.Bot.Frame <= 22.4 * 60 * 3
                && Count(UnitTypes.GATEWAY) >= 3;
        }

        public override string Name()
        {
            return "ThreeGate";
        }
    }
}
