using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class WaitForDetectionController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.IsFlying)
                return false;
            if (agent.Unit.Shield >= agent.Unit.ShieldMax - 10)
                return false;
            Agent closest = null;
            float dist = 1000000;
            foreach (Agent ally in Bot.Main.UnitManager.Agents.Values)
            {
                if (ally.Unit.UnitType != UnitTypes.OBSERVER)
                    continue;
                float newDistance = agent.DistanceSq(ally);
                if (newDistance <= 10 * 10)
                    return false;
                if (newDistance < dist)
                {
                    dist = newDistance;
                    closest = ally;
                }
            }

            if (closest != null)
            {
                agent.Order(Abilities.MOVE, SC2Util.To2D(closest.Unit.Pos));
                return true;
            }

            agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
            return true;
        }
    }
}
