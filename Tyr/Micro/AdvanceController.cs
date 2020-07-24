using SC2APIProtocol;
using Tyr.Agents;
using Tyr.CombatSim;
using Tyr.Util;

namespace Tyr.Micro
{
    public class AdvanceController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.CombatSimulationDecision != CombatSimulationDecision.None
                || Bot.Main.Frame - agent.CombatSimulationDecisionFrame >= 10)
                return false;

            if (agent.Unit.WeaponCooldown == 0)
                return false;

            if (agent.Unit.UnitType == UnitTypes.COLOSUS
                || agent.Unit.UnitType == UnitTypes.TEMPEST
                || agent.Unit.UnitType == UnitTypes.ZEALOT
                || agent.Unit.UnitType == UnitTypes.HIGH_TEMPLAR)
                return false;

            Point2D moveToward = null;
            float dist = 10 * 10;
            int priority = -1;

            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.IsFlying && !agent.CanAttackAir())
                    continue;
                if (!enemy.IsFlying && enemy.UnitType != UnitTypes.COLOSUS && !agent.CanAttackGround())
                    continue;

                int newPriority;
                if (UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    newPriority = 2;
                else if (UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                    newPriority = 3;
                else
                    newPriority = 1;

                if (newPriority < priority)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newPriority > priority || newDist < dist)
                {
                    moveToward = SC2Util.To2D(enemy.Pos);
                    dist = newDist;
                    priority = newPriority;
                }
            }
            if (moveToward != null)
            {
                agent.Order(Abilities.MOVE, moveToward);
                return true;
            }

            return false;
        }
    }
}
