using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class StutterController : CustomController
    {
        public bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.VOID_RAY
                && agent.Unit.UnitType != UnitTypes.ADEPT
                && agent.Unit.UnitType != UnitTypes.STALKER
                && agent.Unit.UnitType != UnitTypes.COLLOSUS
                && agent.Unit.UnitType != UnitTypes.IMMORTAL
                && agent.Unit.UnitType != UnitTypes.ROACH
                && agent.Unit.UnitType != UnitTypes.HYDRALISK
                && agent.Unit.UnitType != UnitTypes.QUEEN
                && agent.Unit.UnitType != UnitTypes.INFESTOR_TERRAN
                && agent.Unit.UnitType != UnitTypes.CORRUPTOR
                && agent.Unit.UnitType != UnitTypes.BROOD_LORD
                && agent.Unit.UnitType != UnitTypes.MUTALISK
                && agent.Unit.UnitType != UnitTypes.RAVAGER
                && agent.Unit.UnitType != UnitTypes.MARINE
                && agent.Unit.UnitType != UnitTypes.SIEGE_TANK
                && agent.Unit.UnitType != UnitTypes.HELLION
                && agent.Unit.UnitType != UnitTypes.HELLBAT)
                return false;

            if (agent.Unit.WeaponCooldown == 0)
                return false;

            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (unit.UnitType == UnitTypes.OVERLORD
                    || unit.UnitType == UnitTypes.OVERSEER
                    || unit.UnitType == UnitTypes.LARVA
                    || unit.UnitType == UnitTypes.EGG)
                    continue;

                if (unit.UnitType != UnitTypes.PHOTON_CANNON
                    && unit.UnitType != UnitTypes.SPINE_CRAWLER
                    && unit.UnitType != UnitTypes.SPORE_CRAWLER
                    && unit.UnitType != UnitTypes.MISSILE_TURRET
                    && unit.UnitType != UnitTypes.PLANETARY_FORTRESS
                    && UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                float maxDist;
                if (agent.Unit.UnitType == UnitTypes.BROOD_LORD)
                    maxDist = 8 * 8;
                else if (agent.Unit.UnitType == UnitTypes.RAVAGER) maxDist = 6 * 6;
                else maxDist = 4 * 4;
                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= maxDist)
                {
                    agent.Order(Abilities.MOVE, SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation));
                    return true;
                }
            }
            return false;
        }
    }
}
