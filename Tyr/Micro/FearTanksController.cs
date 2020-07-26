using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class FearTanksController : CustomController
    {   
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Point2D retreatFrom = null;
            float dist = 15 * 15;

            foreach (UnitLocation enemy in Bot.Main.EnemyTankManager.Tanks)
            {
                if (!Bot.Main.EnemyManager.LastSeenFrame.ContainsKey(enemy.Tag))
                    continue;
                float newDist = agent.DistanceSq(enemy.Pos);
                if (enemy.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
                    continue;

                if (newDist < dist)
                {
                    retreatFrom = SC2Util.To2D(enemy.Pos);
                    dist = newDist;
                }
            }

            if (retreatFrom != null)
            {
                agent.Order(Abilities.MOVE, agent.From(retreatFrom, 4));
                return true;
            }

            return false;
        }
    }
}
