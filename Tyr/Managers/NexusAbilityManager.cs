using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Managers
{
    public class NexusAbilityManager : Manager
    {
        Dictionary<ulong, int> lastChronoFrame = new Dictionary<ulong, int>();
        public HashSet<uint> PriotitizedAbilities = new HashSet<uint>();
        public void OnFrame(Tyr tyr)
        {
            if (tyr.GameInfo.PlayerInfo[(int)tyr.PlayerId - 1].RaceActual != Race.Protoss)
                return;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.NEXUS)
                    findTarget(agent);
        }

        public void findTarget(Agent nexus)
        {
            if (nexus.Unit.Energy < 50)
                return;

            foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                if (agent.IsProductionStructure && agent.Unit.Orders.Count > 0 && PriotitizedAbilities.Contains(agent.Unit.Orders[0].AbilityId) && Tyr.Bot.Frame - lastChrono(agent) >= 500)
                {
                    nexus.Order(3755, agent.Unit.Tag);
                    recordFrame(agent);
                    return;
                }
            foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                if (agent.IsProductionStructure && agent.Unit.Orders.Count > 0 && Tyr.Bot.Frame - lastChrono(agent) >= 500)
                {
                    nexus.Order(3755, agent.Unit.Tag);
                    recordFrame(agent);
                    return;
                }

        }

        private int lastChrono(Agent target)
        {
            if (!lastChronoFrame.ContainsKey(target.Unit.Tag))
                return -500;
            return lastChronoFrame[target.Unit.Tag];
        }

        private void recordFrame(Agent target)
        {
            if (!lastChronoFrame.ContainsKey(target.Unit.Tag))
                lastChronoFrame.Add(target.Unit.Tag, Tyr.Bot.Frame);
            else
                lastChronoFrame[target.Unit.Tag] = Tyr.Bot.Frame;
        }
    }
}
