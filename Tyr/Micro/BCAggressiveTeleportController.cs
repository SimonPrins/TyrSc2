using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class BCAggressiveTeleportController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.BATTLECRUISER)
                return false;

            if (agent.DistanceSq(target) < 30 * 30)
                return false;
            if (Tyr.Bot.Frame % 10 == 0)
            {
                agent.Order(2358, target);
                return true;
            }
            return false;
        }
    }
}
