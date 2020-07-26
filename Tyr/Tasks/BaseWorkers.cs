using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;

namespace SC2Sharp.Tasks
{
    public class BaseWorkers
    {
        public Base Base { get; set; }
        public List<Agent> MineralWorkers = new List<Agent>();
        public List<GasWorkers> GasWorkers = new List<GasWorkers>();
        private HashSet<ulong> mineralTags;

        public int Count
        {
            get
            {
                return MineralWorkers.Count;
            }
        }

        public void OnFrame(Bot bot)
        {
            // Remove dead workers.
            for (int i = Count - 1; i >= 0; i--)
                if (!bot.UnitManager.Agents.ContainsKey(MineralWorkers[i].Unit.Tag))
                {
                    MineralWorkers[i] = MineralWorkers[Count - 1];
                    MineralWorkers.RemoveAt(Count - 1);
                }
            
            mineralTags = new HashSet<ulong>();
            foreach (MineralField mineralField in Base.BaseLocation.MineralFields)
                mineralTags.Add(mineralField.Tag);
            
            if (Base.BaseLocation.MineralFields.Count > 0)
                foreach (Agent mineralWorker in MineralWorkers)
                    if (mineralWorker.Unit.Orders.Count == 0
                                || MiningWrongMineral(mineralWorker))
                        mineralWorker.Order(Abilities.MOVE, Base.BaseLocation.MineralFields[0].Tag);
        }

        private bool MiningWrongMineral(Agent mineralWorker)
        {
            if (mineralTags.Contains(mineralWorker.Unit.Orders[0].TargetUnitTag))
                return false;
            uint ability = mineralWorker.Unit.Orders[0].AbilityId;
            return ability != 299 && ability != 296 && ability != 1184;
        }

        public void Add(Agent agent)
        {
            MineralWorkers.Add(agent);
            if (Base.BaseLocation.MineralFields.Count > 0)
                agent.Order(Abilities.MOVE, Base.BaseLocation.MineralFields[0].Tag);
        }

        public Agent Pop()
        {
            Agent agent = null;
            if (MineralWorkers.Count > 0)
            {
                agent = MineralWorkers[MineralWorkers.Count - 1];
                MineralWorkers.RemoveAt(MineralWorkers.Count - 1);
            }

            if (agent == null)
            {
                foreach (GasWorkers gasWorkers in GasWorkers)
                {
                    if (gasWorkers.Count > 0)
                    {
                        agent = gasWorkers.Workers[gasWorkers.Workers.Count - 1];
                        gasWorkers.Workers.RemoveAt(gasWorkers.Workers.Count - 1);
                    }
                }
            }
            return agent;
        }
    }
}
