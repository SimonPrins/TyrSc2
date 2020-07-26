using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class TargetFireController : CustomController
    {
        public PriorityTargetting PriorityTargetting;
        public bool MoveWhenNoTarget = true;

        public TargetFireController(PriorityTargetting priorityTargetting)
        {
            PriorityTargetting = priorityTargetting;
        }

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.WeaponCooldown == 0)
                return false;

            Unit killTarget = PriorityTargetting.GetTarget(agent);
            if (killTarget == null)
            {
                agent.Order(Abilities.MOVE, target);
                return true;
            }
            else if (MoveWhenNoTarget)
            {
                agent.Order(Abilities.ATTACK, killTarget.Tag);
                return true;
            }
            return false;
        }
    }
}
