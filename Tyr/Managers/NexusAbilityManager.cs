using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class NexusAbilityManager : Manager
    {
        Dictionary<ulong, int> lastChronoFrame = new Dictionary<ulong, int>();
        public HashSet<uint> PriotitizedAbilities = new HashSet<uint>();
        public bool OnlyChronoPrioritizedUnits = false;

        public bool Stopped = false;

        private Dictionary<ulong, int> NotReadyFrame = new Dictionary<ulong, int>();

        public void OnFrame(Bot bot)
        {
            if (bot.GameInfo.PlayerInfo[(int)bot.PlayerId - 1].RaceActual != Race.Protoss
                || Stopped)
                return;
            foreach (Agent agent in bot.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.NEXUS)
                {
                    if (agent.Unit.BuildProgress <= 0.999)
                    {
                        if (NotReadyFrame.ContainsKey(agent.Unit.Tag))
                            NotReadyFrame[agent.Unit.Tag] = bot.Frame;
                        else
                            NotReadyFrame.Add(agent.Unit.Tag, bot.Frame);
                        continue;
                    }
                    if (NotReadyFrame.ContainsKey(agent.Unit.Tag)
                        && bot.Frame - NotReadyFrame[agent.Unit.Tag] < 4)
                        continue;
                    findTarget(agent);
                }
        }

        public void findTarget(Agent nexus)
        {
            if (nexus.Unit.Energy < 50)
                return;
            if (Bot.Main.UnitManager.Completed(UnitTypes.PYLON) == 0)
                return;

            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                if (agent.IsProductionStructure && agent.Unit.Orders.Count > 0 && PriotitizedAbilities.Contains(agent.Unit.Orders[0].AbilityId) && Bot.Main.Frame - lastChrono(agent) >= 20 * 22.4)
                {
                    nexus.Order(3755, agent.Unit.Tag);
                    recordFrame(agent);
                    return;
                }
            if (!OnlyChronoPrioritizedUnits)
                foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                    if (agent.IsProductionStructure && agent.Unit.Orders.Count > 0 && Bot.Main.Frame - lastChrono(agent) >= 20 * 22.4)
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
                lastChronoFrame.Add(target.Unit.Tag, Bot.Main.Frame);
            else
                lastChronoFrame[target.Unit.Tag] = Bot.Main.Frame;
        }
    }
}
