using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class EvadeCannonsController : CustomController
    {
        public Point2D FleeToward = null;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType == UnitTypes.THOR)
                return false;

            if (agent.Unit.WeaponCooldown >= 5
                && (Bot.Main.Frame % 100 > 50 || Bot.Main.UnitManager.Completed(UnitTypes.STALKER) + Bot.Main.UnitManager.Completed(UnitTypes.IMMORTAL) >= 8))
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

            float dist = 10 * 10;
            Unit fleeTarget = null;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (unit.UnitType != UnitTypes.PHOTON_CANNON)
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist > dist)
                    continue;
                fleeTarget = unit;
                dist = newDist;
            }
            if (fleeTarget != null)
            {
                PotentialHelper potential = new PotentialHelper(agent.Unit.Pos, 4);
                potential.From(fleeTarget.Pos, 2);
                potential.To(FleeToward);
                agent.Order(Abilities.MOVE, potential.Get());
                return true;
            }
            return false;
        }
    }
}
