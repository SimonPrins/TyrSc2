using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class SkyToss : Strategy
    {
        private static Strategy Singleton = new SkyToss();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return (Bot.Main.EnemyRace == Race.Protoss || Bot.Main.EnemyRace == Race.Random)
                && Count(UnitTypes.CARRIER) + Count(UnitTypes.MOTHERSHIP) + Count(UnitTypes.INTERCEPTOR) > 0;
        }

        public override string Name()
        {
            return "SkyToss";
        }
    }
}
