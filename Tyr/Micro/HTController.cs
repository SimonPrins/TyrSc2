using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class HTController : CustomController
    {
        public bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.HIGH_TEMPLAR)
                return false;
            if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(52))
                return false;
            if (agent.Unit.Energy < 73)
                return false;
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 8 * 8)
                {
                    int count = 0;
                    foreach (Unit unit2 in Tyr.Bot.Enemies())
                    {
                        if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                            continue;

                        if (unit.UnitType == UnitTypes.ZERGLING)
                            continue;

                        if (SC2Util.DistanceSq(unit.Pos, unit2.Pos) <= 3 * 3)
                            count++;
                    }
                    if (count >= 10)
                    {
                        agent.Order(1036, SC2Util.To2D(unit.Pos));
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
