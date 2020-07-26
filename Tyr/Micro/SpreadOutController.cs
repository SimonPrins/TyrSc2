using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class SpreadOutController : CustomController
    {
        public HashSet<uint> SpreadTypes = new HashSet<uint>();
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType == UnitTypes.THOR && agent.Unit.WeaponCooldown >= 5)
                return false;

            if (!SpreadTypes.Contains(agent.Unit.UnitType))
                return false;

            if (agent.Unit.WeaponCooldown == 0)
                return false;

            float dist = 3 * 3;
            Agent away = null;
            foreach (Agent other in Bot.Main.Units())
            {
                if (!SpreadTypes.Contains(other.Unit.UnitType))
                    continue;

                if (other.Unit.Tag == agent.Unit.Tag)
                    continue;

                float newDist = agent.DistanceSq(other);
                if (newDist < dist)
                {
                    away = other;
                    dist = newDist;
                }
            }
            if (away != null)
            {
                agent.Order(Abilities.MOVE, agent.From(away, 4));
                return true;
            }
            return false;
        }
    }
}
