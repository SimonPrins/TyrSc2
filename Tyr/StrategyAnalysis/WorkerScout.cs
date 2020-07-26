using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.StrategyAnalysis
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
            if (Bot.Main.Frame >= 22.4 * 60 * 2.5)
                return false;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, Bot.Main.MapAnalyzer.StartLocation) <= 30 * 30)
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
