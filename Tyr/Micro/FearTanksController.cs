using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Micro
{
    public class FearTanksController : CustomController
    {   
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Point2D retreatFrom = null;
            float dist = 15 * 15;

            foreach (UnitLocation enemy in Bot.Bot.EnemyTankManager.Tanks)
            {
                if (!Bot.Bot.EnemyManager.LastSeenFrame.ContainsKey(enemy.Tag))
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
