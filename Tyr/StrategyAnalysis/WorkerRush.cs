using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class WorkerRush : Strategy
    {
        private static Strategy Singleton = new WorkerRush();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Bot.Bot.Frame >= 22.4 * 60)
                return false;
            int farWorkers = 0;
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(unit.UnitType))
                    continue;

                // See if this worker is far from the enemy base.
                bool far = true;
                foreach (Point2D start in Bot.Bot.TargetManager.PotentialEnemyStartLocations)
                    if (SC2Util.DistanceSq(unit.Pos, start) <= 40 * 40)
                        far = false;

                if (far)
                    farWorkers++;
            }
            return farWorkers >= 5;
        }

        public override string Name()
        {
            return "WorkerRush";
        }
    }
}
