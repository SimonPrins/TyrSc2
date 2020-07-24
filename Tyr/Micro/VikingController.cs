using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class VikingController : CustomController
    {
        public bool StickToTanks = true;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.VIKING_FIGHTER)
                return false;

            float dist;
            if (agent.DistanceSq(Bot.Main.MapAnalyzer.StartLocation) >= 40 * 40
                && StickToTanks)
            {
                Point2D retreatTo = null;
                dist = 15 * 15;
                foreach (Agent ally in Bot.Main.UnitManager.Agents.Values)
                {
                    if (ally.Unit.UnitType != UnitTypes.SIEGE_TANK
                        && ally.Unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
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
            }

            dist = 15 * 15;
            Unit killTarget = null;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!enemy.IsFlying)
                    continue;

                if (killTarget != null
                    && !SecondaryTarget(killTarget.UnitType)
                    && SecondaryTarget(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist
                    || killTarget == null
                    || (SecondaryTarget(killTarget.UnitType) && !SecondaryTarget(enemy.UnitType)))
                {
                    killTarget = enemy;
                    dist = newDist;
                }
            }

            if (killTarget != null
                && !SecondaryTarget(killTarget.UnitType))
            {
                agent.Order(Abilities.ATTACK, killTarget.Tag);
                return true;
            }

            dist = 10 * 10;
            Unit fleeTarget = null;
            foreach (Unit enemy in Bot.Main.Enemies())
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

            if (killTarget != null)
            {
                agent.Order(Abilities.ATTACK, killTarget.Tag);
                return true;
            }

            return false;
        }

        private bool SecondaryTarget(uint type)
        {
            return type == UnitTypes.OVERLORD
                || type == UnitTypes.OVERSEER;
        }
    }
}
