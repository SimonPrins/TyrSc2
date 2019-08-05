using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class FleeCyclonesController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            int lastAttackFrame = Tyr.Bot.EnemyCycloneManager.GetLastHitFrame(agent.Unit.Tag);
            if (Tyr.Bot.Frame - lastAttackFrame >= 5 * 22.4)
                return false;

            Unit fleeCyclone = null;
            float distance = 15.5f * 15.5f;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.CYCLONE)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (distance < newDist)
                    continue;
                distance = newDist;
                fleeCyclone = enemy;
            }

            if (fleeCyclone != null)
            {
                agent.Flee(fleeCyclone.Pos);
                return true;
            }
            
            return false;
        }
    }
}
