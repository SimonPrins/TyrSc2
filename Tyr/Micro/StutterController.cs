using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class StutterController : CustomController
    {
        public Point2D Toward;
        public float Range = -1;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType == UnitTypes.THOR && agent.Unit.WeaponCooldown >= 5)
                return false;

            if (agent.Unit.UnitType != UnitTypes.VOID_RAY
                && agent.Unit.UnitType != UnitTypes.ADEPT
                && agent.Unit.UnitType != UnitTypes.STALKER
                && agent.Unit.UnitType != UnitTypes.COLOSUS
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
                && agent.Unit.UnitType != UnitTypes.MARAUDER
                && agent.Unit.UnitType != UnitTypes.SIEGE_TANK
                && agent.Unit.UnitType != UnitTypes.HELLION
                && agent.Unit.UnitType != UnitTypes.HELLBAT
                && agent.Unit.UnitType != UnitTypes.THOR
                && agent.Unit.UnitType != UnitTypes.THOR_SINGLE_TARGET
                && agent.Unit.UnitType != UnitTypes.CYCLONE)
                return false;

            if (agent.Unit.WeaponCooldown == 0 && agent.Unit.UnitType != UnitTypes.CYCLONE)
                return false;

            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (agent.Unit.UnitType == UnitTypes.HELLBAT
                    && (UnitTypes.RangedTypes.Contains(unit.UnitType)
                    || UnitTypes.BuildingTypes.Contains(unit.UnitType)
                    || UnitTypes.WorkerTypes.Contains(unit.UnitType)))
                    continue;

                if (unit.UnitType == UnitTypes.OVERLORD
                    || unit.UnitType == UnitTypes.OVERSEER
                    || unit.UnitType == UnitTypes.LARVA
                    || unit.UnitType == UnitTypes.EGG
                    || unit.UnitType == UnitTypes.CREEP_TUMOR
                    || unit.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || unit.UnitType == UnitTypes.CREEP_TUMOR_QUEEN)
                    continue;

                if (unit.UnitType != UnitTypes.PHOTON_CANNON
                    && unit.UnitType != UnitTypes.SPINE_CRAWLER
                    && unit.UnitType != UnitTypes.SPORE_CRAWLER
                    && unit.UnitType != UnitTypes.MISSILE_TURRET
                    && unit.UnitType != UnitTypes.PLANETARY_FORTRESS
                    && UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                float maxDist;
                if (Range < 0)
                {
                    if (agent.Unit.UnitType == UnitTypes.BROOD_LORD)
                        maxDist = 8 * 8;
                    else if (agent.Unit.UnitType == UnitTypes.RAVAGER) maxDist = 6 * 6;
                    else maxDist = 4 * 4;
                }
                else maxDist = Range * Range;
                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= maxDist)
                {
                    Point2D stutterTarget = Toward == null ? SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation) : Toward;
                    if (agent.DistanceSq(stutterTarget) > 10 * 10)
                        agent.Order(Abilities.MOVE, stutterTarget);
                    else
                        agent.Order(Abilities.MOVE, agent.From(unit, 4));
                    return true;
                }
            }
            return false;
        }
    }
}
