using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class CorruptorController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.CORRUPTOR)
                return false;
            
            Point2D retreatTo = null;
            float dist = 15 * 15;
            foreach (Agent ally in Bot.Bot.UnitManager.Agents.Values)
            {
                if (ally.Unit.UnitType != UnitTypes.BROOD_LORD)
                    continue;

                float newDist = agent.DistanceSq(ally);
                if (newDist < dist)
                {
                    retreatTo = SC2Util.To2D(ally.Unit.Pos);
                    dist = newDist;
                }
            }
            if (retreatTo != null && dist >= 8 * 8)
            {
                agent.Order(Abilities.MOVE, retreatTo);
                return true;
            }

            dist = 15 * 15;
            Unit killTarget = null;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (!enemy.IsFlying)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    killTarget = enemy;
                    dist = newDist;
                }
            }

            if (killTarget != null)
            {
                agent.Order(Abilities.ATTACK, killTarget.Tag);
                return true;
            }

            dist = 10 * 10;
            Unit fleeTarget = null;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    fleeTarget = enemy;
                    dist = newDist;
                }
            }

            if (fleeTarget != null)
            {
                PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
                potential.Magnitude = 4;
                potential.From(fleeTarget.Pos);
                agent.Order(Abilities.MOVE, potential.Get());
                return true;
            }

            return false;
        }
    }
}
