using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class TempestController : CustomController
    {
        public Point2D RetreatPos;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.TEMPEST)
                return false;

            if (agent.Unit.WeaponCooldown == 0)
                return false;

            float dist = 11 * 11;
            Unit fleeTarget = null;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (!UnitTypes.CanAttackAir(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);

                if (!enemy.IsFlying && newDist >= 9 * 9)
                    continue;
                if (newDist < dist)
                {
                    fleeTarget = enemy;
                    dist = newDist;
                }
            }

            if (fleeTarget != null)
            {
                if (RetreatPos != null)
                    agent.Flee(fleeTarget.Pos, RetreatPos);
                else
                    agent.Flee(fleeTarget.Pos);
                return true;
            }

            return false;
        }
    }
}
