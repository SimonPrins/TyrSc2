using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Tasks
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

        public void OnFrame(Tyr tyr)
        {
            // Remove dead workers.
            for (int i = Workers.Count - 1; i >= 0; i--)
            {
                if (!LastSeen.ContainsKey(Workers[i].Unit.Tag))
                    LastSeen.Add(Workers[i].Unit.Tag, tyr.Frame);
                if (tyr.UnitManager.Agents.ContainsKey(Workers[i].Unit.Tag))
                    LastSeen[Workers[i].Unit.Tag] = tyr.Frame;
                if (tyr.Frame - LastSeen[Workers[i].Unit.Tag] >= 200)
                {
                    tyr.UnitManager.DisappearedUnits.Remove(Workers[i].Unit.Tag);
                    Workers[i] = Workers[Workers.Count - 1];
                    Workers.RemoveAt(Workers.Count - 1);
                }
            }

            foreach (Agent worker in Workers)
                if (tyr.UnitManager.Agents.ContainsKey(worker.Unit.Tag) && 
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
            if (!Tyr.Bot.UnitManager.DisappearedUnits.ContainsKey(agent.Unit.Tag))
                Tyr.Bot.UnitManager.DisappearedUnits.Add(agent.Unit.Tag, agent);
        }
    }
}
