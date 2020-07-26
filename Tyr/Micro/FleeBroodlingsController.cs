using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class FleeBroodlingsController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Unit fleeTarget = null;
            float dist = 6 * 6;
            foreach (Unit enemy in Bot.Main.Enemies())
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

            agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
            return true;
        }
    }
}
