using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class SentryController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.SENTRY)
                return false;
            if (GuardianShield(agent))
                return true;

            if (agent.FleeEnemies(false, 8))
                return true;

            return false;
        }

        private bool GuardianShield(Agent agent)
        {
            if (agent.Unit.BuffIds.Contains(18))
                return false;
            if (agent.Unit.Energy < 75)
                return false;
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 10 * 10
                    && (UnitTypes.RangedTypes.Contains(unit.UnitType) || unit.UnitType == UnitTypes.INTERCEPTOR))
                {
                    agent.Order(76);
                    return true;
                }
            }
            return false;
        }
    }
}
