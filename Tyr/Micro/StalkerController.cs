using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class StalkerController : CustomController
    {
        public bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.STALKER)
                return false;

            if (agent.Unit.Shield <= 1 && !agent.Unit.BuffIds.Contains(3687) && Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(87))
            {
                Unit closestEnemy = null;
                float dist = 12 * 12;
                foreach (Unit enemy in Tyr.Bot.Enemies())
                {
                    if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                        continue;

                    float newDist = SC2Util.DistanceSq(enemy.Pos, agent.Unit.Pos);
                    if (newDist < dist)
                    {
                        dist = newDist;
                        closestEnemy = enemy;
                    }
                }

                if (closestEnemy != null)
                {
                    PotentialHelper potential = new PotentialHelper(agent.Unit.Pos, 2);
                    potential.From(closestEnemy.Pos);
                    agent.Order(Abilities.BLINK, potential.Get());
                    return true;
                }
            }

            Unit kill = null;
            float hp = 10000;
            int priority = 0;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                int newPriority;
                if (enemy.UnitType == UnitTypes.VIKING_FIGHTER
                    || enemy.UnitType == UnitTypes.LIBERATOR
                    || enemy.UnitType == UnitTypes.BANSHEE)
                    newPriority = 5;
                else if (enemy.UnitType == UnitTypes.SIEGE_TANK
                    || enemy.UnitType == UnitTypes.SIEGE_TANK_SIEGED)
                    newPriority = 4;
                else newPriority = 0;
                if (newPriority < priority)
                    continue;
                float newHp = enemy.Health + enemy.Shield;
                if (newPriority == priority && newHp >= hp)
                    continue;
                if (SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos) > 8 * 8)
                    continue;

                kill = enemy;
                priority = newPriority;
                hp = newHp;
            }

            if (kill != null)
            {
                agent.Order(Abilities.ATTACK, kill.Tag);
                return true;
            }

            return false;
        }
    }
}
