using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class DisruptorController : CustomController
    {
        private Dictionary<ulong, int> PhaseFrame = new Dictionary<ulong, int>();

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.DISRUPTOR)
                return false;

            if (PhaseFrame.ContainsKey(agent.Unit.Tag) && Bot.Bot.Frame - PhaseFrame[agent.Unit.Tag] <= 22)
                return true;

            if (Phase(agent))
                return true;

            Unit closestEnemy = null;
            float distance = 9 * 9;
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (!UnitTypes.CanAttackGround(unit.UnitType))
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist >= distance)
                    continue;

                distance = newDist;
                closestEnemy = unit;
            }

            if (closestEnemy == null)
            {
                agent.Order(Abilities.MOVE, target);
                return true;
            }

            agent.Order(Abilities.MOVE, agent.From(closestEnemy, 4));
            return true;
        }

        private bool Phase(Agent agent)
        {
            if (PhaseFrame.ContainsKey(agent.Unit.Tag) && Bot.Bot.Frame - PhaseFrame[agent.Unit.Tag] <= 224)
                return false;

            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                if (agent.Unit.UnitType == UnitTypes.ZERGLING
                    || agent.Unit.UnitType == UnitTypes.BROODLING
                    || agent.Unit.UnitType == UnitTypes.EGG
                    || agent.Unit.UnitType == UnitTypes.LARVA)
                    continue;

                if (unit.IsFlying)
                    continue;

                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) > 10 * 10)
                    continue;

                int count = 0;
                bool closeAlly = false;
                foreach (Agent ally in Bot.Bot.UnitManager.Agents.Values)
                {
                    if (ally.Unit.IsFlying)
                        continue;
                    if (ally.DistanceSq(unit) <= 2 * 2)
                    {
                        closeAlly = true;
                        break;
                    }
                }
                if (closeAlly)
                    break; 
                foreach (Unit unit2 in Bot.Bot.Enemies())
                {
                    if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                        continue;

                    if (unit.UnitType == UnitTypes.ZERGLING
                        || agent.Unit.UnitType == UnitTypes.BROODLING
                        || agent.Unit.UnitType == UnitTypes.EGG
                        || agent.Unit.UnitType == UnitTypes.LARVA)
                        continue;

                    if (unit.IsFlying)
                        continue;

                    if (SC2Util.DistanceSq(unit.Pos, unit2.Pos) <= 3 * 3)
                        count++;
                }
                if (count >= 6)
                {
                    agent.Order(2346, SC2Util.To2D(unit.Pos));
                    CollectionUtil.Add(PhaseFrame, agent.Unit.Tag, Bot.Bot.Frame);
                    return true;
                }

            }
            return false;
        }
    }
}
