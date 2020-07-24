using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Micro
{
    public class HitAndRunController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            bool nearbyDead = false;
            foreach (RecentlyDeceased deceased in Bot.Main.EnemyManager.GetRecentlyDeceased())
            {
                if (UnitTypes.AirAttackTypes.Contains(deceased.UnitType) && agent.DistanceSq(deceased.Pos) <= 15 * 15)
                {
                    nearbyDead = true;
                    break;
                }
            }
            if (!nearbyDead)
                return false;
            
            PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
            potential.Magnitude = 4;

            int count = 0;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < 12 * 12)
                {
                    count++;
                    potential.From(enemy.Pos);
                }
            }
            if (count < 5)
                return false;
            agent.Order(Abilities.MOVE, potential.Get());
            return true;
        }
    }
}
