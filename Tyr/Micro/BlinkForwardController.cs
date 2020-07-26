using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class BlinkForwardController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.STALKER)
                return false;

            if (agent.Unit.BuffIds.Contains(3687) || !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(87))
                return false;

            int closeAllyCount = 0;
            foreach (Agent ally in Bot.Main.UnitManager.Agents.Values)
            {
                if (ally.Unit.UnitType != UnitTypes.STALKER)
                    continue;
                if (agent.DistanceSq(ally) <= 5 * 5)
                    closeAllyCount++;
            }
            if (closeAllyCount < 3)
                return false;

            Unit closestEnemy = null;
            float dist = 8 * 8;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BANSHEE
                    && enemy.UnitType != UnitTypes.CYCLONE
                    && enemy.UnitType != UnitTypes.TEMPEST
                    && enemy.UnitType != UnitTypes.VIKING_FIGHTER)
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
                agent.Order(Abilities.BLINK, closestEnemy.Pos);
                return true;
            }


            return false;
        }
    }
}
