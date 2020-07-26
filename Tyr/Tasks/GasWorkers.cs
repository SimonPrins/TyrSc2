using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;

namespace SC2Sharp.Tasks
{
    public class GasWorkers
    {
        public Base Base { get; set; }
        public Unit Gas { get; set; }
        public List<Agent> Workers = new List<Agent>();
        private Dictionary<ulong, int> LastSeen = new Dictionary<ulong, int>();

        public int Count
        {
            get
            {
                return Workers.Count;
            }
        }

        public void OnFrame(Bot bot)
        {
            // Remove dead workers.
            for (int i = Workers.Count - 1; i >= 0; i--)
            {
                if (!LastSeen.ContainsKey(Workers[i].Unit.Tag))
                    LastSeen.Add(Workers[i].Unit.Tag, bot.Frame);
                if (bot.UnitManager.Agents.ContainsKey(Workers[i].Unit.Tag))
                    LastSeen[Workers[i].Unit.Tag] = bot.Frame;
                if (bot.Frame - LastSeen[Workers[i].Unit.Tag] >= 200)
                {
                    bot.UnitManager.DisappearedUnits.Remove(Workers[i].Unit.Tag);
                    Workers[i] = Workers[Workers.Count - 1];
                    Workers.RemoveAt(Workers.Count - 1);
                }
            }

            foreach (Agent worker in Workers)
                if (bot.UnitManager.Agents.ContainsKey(worker.Unit.Tag) && 
                    (worker.Unit.Orders.Count == 0 || MiningWrongGas(worker)))
                    worker.Order(Abilities.MOVE, Gas.Tag);
        }

        private bool MiningWrongGas(Agent worker)
        {
            if (Gas.Tag == worker.Unit.Orders[0].TargetUnitTag)
                return false;
            uint ability = worker.Unit.Orders[0].AbilityId;
            return ability == 298 || ability == 295 || ability == 1183;
        }

        public void Add(Agent agent)
        {
            Workers.Add(agent);
            agent.Order(Abilities.MOVE, Gas.Tag);
            if (!Bot.Main.UnitManager.DisappearedUnits.ContainsKey(agent.Unit.Tag))
                Bot.Main.UnitManager.DisappearedUnits.Add(agent.Unit.Tag, agent);
        }
    }
}
