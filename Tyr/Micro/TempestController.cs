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

            float dist = 12 * 12;
            Unit fleeTarget = null;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (!UnitTypes.CanAttackAir(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);

                if (!enemy.IsFlying && newDist >= 10.5 * 10.5)
                    continue;
                if (newDist >= dist)
                    continue;
                fleeTarget = enemy;
                dist = newDist;
            }

            if (fleeTarget != null)
            {
                Bot.Bot.DrawLine(agent, fleeTarget.Pos);
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
