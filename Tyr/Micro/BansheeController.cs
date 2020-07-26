using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;

namespace SC2Sharp.Micro
{
    public class BansheeController : CustomController
    {
        private Dictionary<ulong, int> LockOnFrame = new Dictionary<ulong, int>();

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.BANSHEE)
                return false;

            if (Bot.Main.Frame % 22 == 0)
            {
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (!UnitTypes.CanAttackAir(enemy.UnitType))
                        continue;
                    if (agent.DistanceSq(enemy) <= 12 * 12)
                    {
                        agent.Order(392);
                        return true;
                    }
                }
            }
            Unit targetWorker = null;
            float distance = 9 * 9;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.IsFlying)
                    continue;
                bool isWorker = UnitTypes.WorkerTypes.Contains(enemy.UnitType);
                bool isAirAttacker = UnitTypes.AirAttackTypes.Contains(enemy.UnitType);
                if (!isAirAttacker && !isWorker)
                    continue;

                float newDistance = agent.DistanceSq(enemy);
                if (isWorker && newDistance <= distance)
                {
                    distance = newDistance;
                    targetWorker = enemy;
                }
                else if (isAirAttacker && newDistance <= 9 * 9)
                    return false;

            }
            if (targetWorker != null)
            {
                agent.Order(Abilities.ATTACK, targetWorker.Tag);
                return true;
            }

            return false;
        }
    }
}
