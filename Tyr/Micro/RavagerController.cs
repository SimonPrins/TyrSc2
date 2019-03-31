using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class RavagerController : CustomController
    {
        public int Range = 8;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.RAVAGER)
                return false;

            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType) && unit.UnitType != UnitTypes.SPINE_CRAWLER && unit.UnitType != UnitTypes.SPINE_CRAWLER_UPROOTED)
                    continue;

                if (unit.UnitType == UnitTypes.BROODLING
                    || unit.UnitType == UnitTypes.ZERGLING
                    || unit.UnitType == UnitTypes.LARVA
                    || unit.UnitType == UnitTypes.EGG)
                    continue;

                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) > Range * Range)
                    continue;

                int count;
                if (unit.UnitType == UnitTypes.BROOD_LORD || unit.UnitType == UnitTypes.SPINE_CRAWLER)
                    count = 6;
                else
                    count = 1;
                foreach (Unit unit2 in Tyr.Bot.Enemies())
                {
                    if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                        continue;

                    if (unit.UnitType == UnitTypes.BROODLING
                        || unit.UnitType == UnitTypes.ZERGLING
                        || unit.UnitType == UnitTypes.LARVA
                        || unit.UnitType == UnitTypes.EGG)
                        continue;

                    if (SC2Util.DistanceSq(unit.Pos, unit2.Pos) > 2 * 2)
                        continue;

                    if (unit.UnitType == UnitTypes.BROOD_LORD || unit.UnitType == UnitTypes.SPINE_CRAWLER)
                        count += 6;
                    else
                        count++;
                    if (count >= 6)
                        break;
                }
                if (count < 6)
                    continue;

                bool friendlyFire = false;
                foreach (Agent ally in Tyr.Bot.UnitManager.Agents.Values)
                {
                    if (ally.DistanceSq(unit) <= 2 * 2)
                    {
                        friendlyFire = true;
                        break;
                    }
                }

                if (friendlyFire)
                    continue;

                agent.Order(Abilities.CORROSIVE_BILE, SC2Util.To2D(unit.Pos));
                return true;
                
            }
            return false;
        }
    }
}
