using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class HellionHarassController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.HELLION)
                return false;

            if (agent.Unit.WeaponCooldown == 0)
                return false;
            
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (unit.UnitType != UnitTypes.ZERGLING)
                    continue;

                float dist = agent.DistanceSq(unit);
                if (agent.DistanceSq(unit) < 4 * 4)
                {
                    if (agent.DistanceSq(Bot.Bot.MapAnalyzer.StartLocation) > 10 * 10)
                        agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                    else
                        agent.Order(Abilities.MOVE, agent.From(unit, 4));
                }
            }

            float distance = 8 * 8;
            Unit killTarget = null;
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (unit.UnitType != UnitTypes.DRONE
                    && unit.UnitType != UnitTypes.SCV
                    && unit.UnitType != UnitTypes.PROBE
                    && unit.UnitType != UnitTypes.QUEEN)
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist >= distance)
                    continue;
                distance = newDist;
                killTarget = unit;
            }

            if (killTarget != null)
            {
                agent.Order(Abilities.MOVE, agent.Toward(killTarget, 4));
                return true;
            }
            
            return false;
        }
    }
}
