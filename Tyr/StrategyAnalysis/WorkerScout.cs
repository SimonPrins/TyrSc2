using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class WorkerScout : Strategy
    {
        private static WorkerScout Singleton = new WorkerScout();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Bot.Bot.Frame >= 22.4 * 60 * 2.5)
                return false;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, Bot.Bot.MapAnalyzer.StartLocation) <= 30 * 30)
                    return true;
            }
            return false;
        }

        public override string Name()
        {
            return "WorkerScout";
        }
    }
}
