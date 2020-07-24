using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class HellionHarassVTerranController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.HELLION)
                return false;
            
            if (agent.DistanceSq(Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]) >= 12 * 12)
            {
                agent.Order(Abilities.MOVE, Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]);
                return true;
            }

            float distance = 8 * 8;
            Unit killTarget = null;
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (unit.UnitType != UnitTypes.SCV
                    && unit.UnitType != UnitTypes.MARINE
                    && unit.UnitType != UnitTypes.REAPER)
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist >= distance)
                    continue;
                distance = newDist;
                killTarget = unit;
            }

            if (killTarget != null)
            {
                if (agent.Unit.WeaponCooldown == 0)
                    return false;
                else if (distance <= 4 * 4)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                agent.Order(Abilities.MOVE, agent.Toward(killTarget, 4));
                return true;
            }

            agent.Order(Abilities.MOVE, Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]);
            return true;
        }
    }
}
