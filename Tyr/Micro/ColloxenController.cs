using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class ColloxenController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.COLOSUS)
                return false;

            int count = 0;
            Point2D retreatTo = SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation);
            float dist = 1000000000f;
            foreach (Agent ally in Bot.Main.UnitManager.Agents.Values)
            {
                if (ally.Unit.UnitType == UnitTypes.COLOSUS)
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
