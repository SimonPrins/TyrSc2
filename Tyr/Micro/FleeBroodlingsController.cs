using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class FleeBroodlingsController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Unit fleeTarget = null;
            float dist = 6 * 6;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BROODLING)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    dist = newDist;
                    fleeTarget = enemy;
                }
            }

            if (fleeTarget == null)
                return false;

            agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
            return true;
        }
    }
}
