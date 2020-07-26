using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class CycloneVsBansheeController : CustomController
    {
        private Dictionary<ulong, int> LockOnFrame = new Dictionary<ulong, int>();

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.CYCLONE)
                return false;
            
            Unit closeBanshee = null;
            float dist = 6.5f * 6.5f;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BANSHEE)
                    continue;
                float newDist = agent.DistanceSq(enemy);
                if (newDist <= dist)
                {
                    closeBanshee = enemy;
                    dist = newDist;
                }
            }

            if (closeBanshee == null)
                return false;

            dist = 12 * 12;
            Agent fleeTarget = null;
            foreach (Agent turret in Bot.Main.UnitManager.Agents.Values)
            {
                if (turret.Unit.UnitType != UnitTypes.MISSILE_TURRET || turret.Unit.BuildProgress < 0.99)
                    continue;

                float newDist = agent.DistanceSq(turret);
                if (newDist < dist)
                {
                    dist = newDist;
                    fleeTarget = turret;
                }
            }

            if (fleeTarget != null && dist >= 3 * 3)
            {
                agent.Order(Abilities.MOVE, SC2Util.To2D(fleeTarget.Unit.Pos));
                return true;
            } else if (closeBanshee != null)
            {
                agent.Order(Abilities.MOVE, agent.From(closeBanshee, 4));
            }

            return false;
        }
    }
}
