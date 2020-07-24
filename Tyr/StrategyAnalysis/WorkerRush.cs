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
            if (Bot.Main.Frame >= 22.4 * 60)
                return false;
            int farWorkers = 0;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(unit.UnitType))
                    continue;

                // See if this worker is far from the enemy base.
                bool far = true;
                foreach (Point2D start in Bot.Main.TargetManager.PotentialEnemyStartLocations)
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
