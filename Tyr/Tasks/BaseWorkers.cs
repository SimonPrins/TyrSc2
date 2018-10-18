using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.MapAnalysis;

namespace Tyr.Tasks
{
    public class BaseWorkers
    {
        public static int WorkersPerGas = 3;

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

        public void OnFrame(Tyr tyr)
        {
            HashSet<ulong> servedGasses = new HashSet<ulong>();
            for (int i = GasWorkers.Count - 1; i >= 0; i--)
            {
                GasWorkers gasWorkers = GasWorkers[i];
                servedGasses.Add(gasWorkers.Gas.Tag);
                if (!tyr.UnitManager.Agents.ContainsKey(gasWorkers.Gas.Tag) || tyr.UnitManager.Agents[gasWorkers.Gas.Tag].Unit.VespeneContents <= 0)
                {
                    foreach (Agent worker in gasWorkers.Workers)
                    {
                        Add(worker);
                        tyr.UnitManager.DisappearedUnits.Remove(worker.Unit.Tag);
                    }
                    GasWorkers[i] = GasWorkers[GasWorkers.Count - 1];
                    GasWorkers.RemoveAt(GasWorkers.Count - 1);
                } else
                    gasWorkers.Gas = tyr.UnitManager.Agents[gasWorkers.Gas.Tag].Unit;
            }

            foreach (Gas gas in Base.BaseLocation.Gasses)
                if (gas.CanBeGathered && !servedGasses.Contains(gas.Tag) && gas.Unit.BuildProgress > 0.9)
                    GasWorkers.Add(new GasWorkers() { Base = Base, Gas = gas.Unit});

            foreach (GasWorkers gasWorkers in GasWorkers)
            {
                while (gasWorkers.Count < WorkersPerGas && MineralWorkers.Count > 0)
                {
                    gasWorkers.Add(MineralWorkers[MineralWorkers.Count - 1]);
                    MineralWorkers.RemoveAt(MineralWorkers.Count - 1);
                }
                while (gasWorkers.Count > WorkersPerGas && gasWorkers.Count > 0)
                {
                    MineralWorkers.Add(gasWorkers.Workers[gasWorkers.Workers.Count - 1]);
                    gasWorkers.Workers.RemoveAt(gasWorkers.Workers.Count - 1);
                }
                gasWorkers.OnFrame(tyr);
            }

            // Remove dead workers.
            for (int i = Count - 1; i >= 0; i--)
                if (!tyr.UnitManager.Agents.ContainsKey(MineralWorkers[i].Unit.Tag))
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
                                || miningWrongMineral(mineralWorker))
                        mineralWorker.Order(Abilities.MOVE, Base.BaseLocation.MineralFields[0].Tag);
        }

        private bool miningWrongMineral(Agent mineralWorker)
        {
            if (mineralTags.Contains(mineralWorker.Unit.Orders[0].TargetUnitTag))
                return false;
            uint ability = mineralWorker.Unit.Orders[0].AbilityId;
            return ability == 298 || ability == 295 || ability == 1183;
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
