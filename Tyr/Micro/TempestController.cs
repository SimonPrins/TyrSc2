using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class TempestController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.TEMPEST)
                return false;

            if (agent.Unit.WeaponCooldown == 0)
                return false;

            float dist = 9 * 9;
            Unit fleeTarget = null;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (!UnitTypes.CanAttackAir(enemy.UnitType))
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
                agent.Order(Abilities.MOVE, agent.From(fleeTarget.Pos, 4));
                return true;
            }

            return false;
        }
    }
}
