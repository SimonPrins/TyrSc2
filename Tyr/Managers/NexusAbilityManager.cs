using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Managers
{
    public class NexusAbilityManager : Manager
    {
        Dictionary<ulong, int> lastChronoFrame = new Dictionary<ulong, int>();
        public HashSet<uint> PriotitizedAbilities = new HashSet<uint>();
        public bool OnlyChronoPrioritizedUnits = false;

        public bool Stopped = false;

        private Dictionary<ulong, int> NotReadyFrame = new Dictionary<ulong, int>();

        public void OnFrame(Bot tyr)
        {
            if (tyr.GameInfo.PlayerInfo[(int)tyr.PlayerId - 1].RaceActual != Race.Protoss
                || Stopped)
                return;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.NEXUS)
                {
                    if (agent.Unit.BuildProgress <= 0.999)
                    {
                        if (NotReadyFrame.ContainsKey(agent.Unit.Tag))
                            NotReadyFrame[agent.Unit.Tag] = tyr.Frame;
                        else
                            NotReadyFrame.Add(agent.Unit.Tag, tyr.Frame);
                        continue;
                    }
                    if (NotReadyFrame.ContainsKey(agent.Unit.Tag)
                        && tyr.Frame - NotReadyFrame[agent.Unit.Tag] < 4)
                        continue;
                    findTarget(agent);
                }
        }

        public void findTarget(Agent nexus)
        {
            if (nexus.Unit.Energy < 50)
                return;
            if (Bot.Bot.UnitManager.Completed(UnitTypes.PYLON) == 0)
                return;

            foreach (Agent agent in Bot.Bot.UnitManager.Agents.Values)
                if (agent.IsProductionStructure && agent.Unit.Orders.Count > 0 && PriotitizedAbilities.Contains(agent.Unit.Orders[0].AbilityId) && Bot.Bot.Frame - lastChrono(agent) >= 20 * 22.4)
                {
                    nexus.Order(3755, agent.Unit.Tag);
                    recordFrame(agent);
                    return;
                }
            if (!OnlyChronoPrioritizedUnits)
                foreach (Agent agent in Bot.Bot.UnitManager.Agents.Values)
                    if (agent.IsProductionStructure && agent.Unit.Orders.Count > 0 && Bot.Bot.Frame - lastChrono(agent) >= 20 * 22.4)
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
                lastChronoFrame.Add(target.Unit.Tag, Bot.Bot.Frame);
            else
                lastChronoFrame[target.Unit.Tag] = Bot.Bot.Frame;
        }
    }
}
