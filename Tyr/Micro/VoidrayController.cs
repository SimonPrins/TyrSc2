using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class VoidrayController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.VOID_RAY)
                return false;
            if (agent.Unit.BuffIds.Contains(122))
                return false;
            foreach (Unit unit in Bot.Main.Observation.Observation.RawData.Units)
            {
                if (unit.Alliance != Alliance.Enemy)
                    continue;

                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 8 * 8
                    && UnitTypes.LookUp[unit.UnitType].Attributes.Contains(Attribute.Armored)
                    && UnitTypes.AirAttackTypes.Contains(unit.UnitType))
                {
                    agent.Order(2393);
                    return true;
                }
            }
            return false;
        }
    }
}
