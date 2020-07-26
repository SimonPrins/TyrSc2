using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class FearMinesController : CustomController
    {   
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Point2D retreatFrom = null;
            float dist = 10 * 10;

            foreach (UnitLocation enemy in Bot.Main.EnemyMineManager.Mines)
            {
                if (!Bot.Main.EnemyManager.LastSeenFrame.ContainsKey(enemy.Tag))
                    continue;
                float newDist = agent.DistanceSq(enemy.Pos);
                if (Bot.Main.Frame - Bot.Main.EnemyManager.LastSeenFrame[enemy.Tag] < 2)
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
