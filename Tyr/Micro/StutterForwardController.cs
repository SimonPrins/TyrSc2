using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class StutterForwardController : CustomController
    {
        public float MaxDist = 5;
        public bool TowardEnemies = false;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType == UnitTypes.THOR && agent.Unit.WeaponCooldown >= 5)
                return false;

            if (agent.Unit.UnitType != UnitTypes.VOID_RAY
                && agent.Unit.UnitType != UnitTypes.ADEPT
                && agent.Unit.UnitType != UnitTypes.STALKER
                && agent.Unit.UnitType != UnitTypes.COLOSUS
                && agent.Unit.UnitType != UnitTypes.IMMORTAL
                && agent.Unit.UnitType != UnitTypes.ROACH
                && agent.Unit.UnitType != UnitTypes.HYDRALISK
                && agent.Unit.UnitType != UnitTypes.QUEEN
                && agent.Unit.UnitType != UnitTypes.INFESTOR_TERRAN
                && agent.Unit.UnitType != UnitTypes.CORRUPTOR
                && agent.Unit.UnitType != UnitTypes.BROOD_LORD
                && agent.Unit.UnitType != UnitTypes.MUTALISK
                && agent.Unit.UnitType != UnitTypes.RAVAGER
                && agent.Unit.UnitType != UnitTypes.MARINE
                && agent.Unit.UnitType != UnitTypes.MARAUDER
                && agent.Unit.UnitType != UnitTypes.SIEGE_TANK
                && agent.Unit.UnitType != UnitTypes.HELLION
                && agent.Unit.UnitType != UnitTypes.HELLBAT
                && agent.Unit.UnitType != UnitTypes.THOR
                && agent.Unit.UnitType != UnitTypes.THOR_SINGLE_TARGET
                && agent.Unit.UnitType != UnitTypes.CYCLONE)
                return false;

            if (agent.Unit.WeaponCooldown == 0 && agent.Unit.UnitType != UnitTypes.CYCLONE)
                return false;

            if (agent.DistanceSq(target) < MaxDist * MaxDist)
                return false;

            if (TowardEnemies)
            {

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
            }

            agent.Order(Abilities.MOVE, target);
            return true;
        }
    }
}
