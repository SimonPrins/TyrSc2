using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class MedivacController : CustomController
    {
        Dictionary<ulong, ulong> HealTargets = new Dictionary<ulong, ulong>();
        
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.MEDIVAC)
                return false;


            float dist = 10 * 10;
            Unit fleeTarget = null;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (!UnitTypes.CanAttackAir(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    fleeTarget = enemy;
                    dist = newDist;
                }
            }
            if (fleeTarget != null)
            {
                agent.Order(Abilities.MOVE, agent.From(fleeTarget, 4));
                return true;
            }

            if (HealTargets.ContainsKey(agent.Unit.Tag))
            {
                ulong targetTag = HealTargets[agent.Unit.Tag];
                if (Tyr.Bot.UnitManager.Agents.ContainsKey(targetTag)
                    && Tyr.Bot.UnitManager.Agents[targetTag].DistanceSq(agent) <= 7 * 7)
                {
                    agent.Order(386, targetTag);
                    return true;
                }
                else
                    HealTargets.Remove(agent.Unit.Tag);
            }

            foreach (Agent ally in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (!UnitTypes.LookUp[ally.Unit.UnitType].Attributes.Contains(Attribute.Biological))
                    continue;

                if (ally.Unit.Health >= ally.Unit.HealthMax)
                    continue;

                if (ally.DistanceSq(agent) >= 6 * 6)
                    continue;

                if (HealTargets.ContainsKey(agent.Unit.Tag))
                    HealTargets[agent.Unit.Tag] = ally.Unit.Tag;
                else
                    HealTargets.Add(agent.Unit.Tag, ally.Unit.Tag);

                agent.Order(386, ally.Unit.Tag);
                return true;
            }
            
            agent.Order(Abilities.MOVE, target);
            return true;
        }
    }
}
