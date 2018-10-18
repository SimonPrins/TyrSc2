using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class ColloxenController : CustomController
    {
        public bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.COLLOSUS)
                return false;

            int count = 0;
            Point2D retreatTo = SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation);
            float dist = 1000000000f;
            foreach (Agent ally in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (ally.Unit.UnitType == UnitTypes.COLLOSUS)
                    continue;
                if (!UnitTypes.CombatUnitTypes.Contains(ally.Unit.UnitType))
                    continue;

                float newDist = SC2Util.DistanceSq(ally.Unit.Pos, agent.Unit.Pos);
                if (newDist <= 6 * 6)
                    count++;
                if (newDist < dist)
                {
                    retreatTo = SC2Util.To2D(ally.Unit.Pos);
                    dist = newDist;
                }
            }

            if (count < 5 && SC2Util.DistanceSq(agent.Unit.Pos, retreatTo) >= 2 * 2)
            {
                agent.Order(Abilities.MOVE, retreatTo);
                return true;
            }

            return false;
        }
    }
}
