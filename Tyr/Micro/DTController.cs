using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class DTController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.DARK_TEMPLAR)
                return false;

            float dist = 10 * 10;
            Unit detectTarget = null;
            foreach (Unit enemy in Bot.Main.CloakedEnemies())
            {
                if (enemy.UnitType != UnitTypes.OBSERVER)
                    continue;

                float newDist = agent.DistanceSq(enemy);

                if (newDist < dist)
                {
                    detectTarget = enemy;
                    dist = newDist;
                }
            }
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.PHOTON_CANNON)
                    continue;

                float newDist = agent.DistanceSq(enemy);

                if (newDist < dist)
                {
                    detectTarget = enemy;
                    dist = newDist;
                }
            }
            if (detectTarget == null)
                return false;

            dist = 10 * 10;
            Unit fleeTarget = null;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.CanAttackGround(enemy.UnitType))
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
                agent.Flee(fleeTarget.Pos);
                return true;
            }

            return false;
        }
    }
}
