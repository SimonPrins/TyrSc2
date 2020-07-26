using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class TargetUnguardedBuildingsController : CustomController
    {
        public Point2D Toward;
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

            Unit closeCannon = null;
            float dist = 9 * 9;

            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (unit.UnitType != UnitTypes.PHOTON_CANNON)
                    continue;
                if (unit.BuildProgress < 0.9)
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist > dist)
                    continue;
                dist = newDist;
                closeCannon = unit;
            }
            if (closeCannon == null)
                return false;

            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (unit.UnitType == UnitTypes.PHOTON_CANNON)
                    continue;
                if (!UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist > dist)
                    continue;
                agent.Order(Abilities.MOVE, agent.From(closeCannon, 4));
                return true;
            }
            return false;
        }
    }
}
