using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
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
            foreach (Unit unit in Bot.Main.Enemies())
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
