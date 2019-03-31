using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class MarineHarassController : CustomController
    {
        public bool Disabled = false;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (Disabled)
                return false;
            if (agent.Unit.UnitType != UnitTypes.MARINE)
                return false;

            if (agent.Unit.WeaponCooldown == 0)
                return false;

            float distance = 12 * 12;
            Unit killTarget = null;
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(unit.UnitType))
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist >= distance)
                    continue;
                distance = newDist;
                killTarget = unit;
            }

            if (killTarget != null)
            {
                agent.Order(Abilities.MOVE, agent.Toward(killTarget, 4));
                return true;
            }
            
            return false;
        }
    }
}
