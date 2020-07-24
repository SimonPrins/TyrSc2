using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class MothershipController : CustomController
    {
        public Point2D RetreatPos;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.MOTHERSHIP)
                return false;

            float dist = 14 * 14;
            Unit fleeTarget = null;
            foreach (Unit enemy in Bot.Bot.Enemies())
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
                if (RetreatPos != null)
                    agent.Flee(fleeTarget.Pos, RetreatPos);
                else
                    agent.Flee(fleeTarget.Pos);
                return true;
            }

            dist = 10 * 10;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (UnitTypes.CanAttackAir(enemy.UnitType))
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
