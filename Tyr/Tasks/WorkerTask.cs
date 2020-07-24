using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Tasks
{
    public class WorkerTask : Task
    {
        public static WorkerTask Task = new WorkerTask();
        private List<BaseWorkers> baseWorkers;
        private List<Agent> unassignedWorkers = new List<Agent>();
        public bool StopTransfers = false;
        public bool EvacuateThreatenedBases = false;

        public WorkerTask() : base(5)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            if (baseWorkers == null)
            {
                baseWorkers = new List<BaseWorkers>();
                foreach (Base b in tyr.BaseManager.Bases)
                    baseWorkers.Add(new BaseWorkers() { Base = b });
            }

            List<BaseWorkers> myBases = new List<BaseWorkers>();
            foreach (BaseWorkers workers in baseWorkers)
            {
                if (workers.Base.BaseLocation.MineralFields.Count == 0 || workers.Base.ResourceCenter == null || workers.Base.ResourceCenter.Unit.BuildProgress < 0.9)
                {
                    foreach (Agent agent in workers.MineralWorkers)
                        unassignedWorkers.Add(agent);
                    workers.MineralWorkers = new List<Agent>();
                }
                else if (!workers.Base.UnderAttack || StopTransfers)
                {
                    myBases.Add(workers);
                }
            }

            if (myBases.Count > 0 && EvacuateThreatenedBases)
            {
                foreach (BaseWorkers workers in baseWorkers)
                {
                    if (workers.Base.Evacuate)
                    {
                        foreach (Agent agent in workers.MineralWorkers)
                            unassignedWorkers.Add(agent);
                        workers.MineralWorkers = new List<Agent>();
                    }
                }
            }

            if (!StopTransfers && myBases.Count > 0)
                TransferWorkers(myBases);
            else
            {
                for (int i = unassignedWorkers.Count - 1; i >= 0; i--)
                {
                    BaseWorkers closest = null;
                    float distance = 1000 * 1000;
                    foreach (BaseWorkers workers in myBases)
                    {
                        float newDist = unassignedWorkers[i].DistanceSq(workers.Base.BaseLocation.Pos);
                        if (newDist < distance)
                        {
                            distance = newDist;
                            closest = workers;
                        }
                    }
                    if (closest != null)
                    {
                        closest.Add(unassignedWorkers[i]);
                        unassignedWorkers.RemoveAt(i);
                    }
                }

            }

            foreach (BaseWorkers workers in myBases)
                workers.OnFrame(tyr);
        }

        private void TransferWorkers(List<BaseWorkers> myBases)
        {
            if (myBases.Count > 0)
            {
                while (unassignedWorkers.Count > 0)
                {
                    float dist = 10000000;
                    BaseWorkers picked = null;
                    foreach (BaseWorkers myBase in myBases)
                    {
                        float newDist = unassignedWorkers[unassignedWorkers.Count - 1].DistanceSq(myBase.Base.BaseLocation.Pos);
                        if (newDist >= dist)
                            continue;
                        dist = newDist;
                        picked = myBase;
                    }
                    picked.Add(unassignedWorkers[unassignedWorkers.Count - 1]);
                    unassignedWorkers.RemoveAt(unassignedWorkers.Count - 1);
                }
            }

            int workersPerBase;
            if (myBases.Count == 0)
                workersPerBase = 0;
            else
            {
                bool done = false;
                int totalMineralWorkers = units.Count;
                workersPerBase = totalMineralWorkers / myBases.Count;
                while (!done)
                {
                    int notFull = 0;
                    int remaining = totalMineralWorkers;
                    foreach (BaseWorkers workers in myBases)
                    {
                        if (workersPerBase < workers.Base.BaseLocation.MineralFields.Count * 2)
                        {
                            notFull++;
                            remaining -= workersPerBase;
                        }
                        else
                            remaining -= workers.Base.BaseLocation.MineralFields.Count * 2;
                    }
                    if (notFull == 0)
                        done = true;
                    else if (remaining > 0)
                        workersPerBase += Math.Max(1, remaining / notFull);
                    else
                        done = true;
                }
            }

            bool saturated = true;
            foreach (BaseWorkers workers in myBases)
                if (workers.Count < workers.Base.BaseLocation.MineralFields.Count * 2)
                {
                    saturated = false;
                    break;
                }

            foreach (BaseWorkers workers in myBases)
            {
                while (workers.MineralWorkers.Count > 0 &&
                    (workers.Count > workersPerBase + 2
                    || (workers.Count > workers.Base.BaseLocation.MineralFields.Count * 2 && !saturated)))
                {
                    unassignedWorkers.Add(workers.MineralWorkers[workers.Count - 1]);
                    workers.MineralWorkers.RemoveAt(workers.Count - 1);
                }
            }

            foreach (BaseWorkers workers in myBases)
            {
                while (workers.Count < workersPerBase + 1
                    && unassignedWorkers.Count > 0
                    && workers.Count < workers.Base.BaseLocation.MineralFields.Count * 2)
                {
                    workers.Add(unassignedWorkers[unassignedWorkers.Count - 1]);
                    unassignedWorkers.RemoveAt(unassignedWorkers.Count - 1);
                }
            }
            
            /*
            for (int i = 0; myBases.Count > 0 && unassignedWorkers.Count > 0; i++)
            {
                myBases[i % myBases.Count].Add(unassignedWorkers[unassignedWorkers.Count - 1]);
                unassignedWorkers.RemoveAt(unassignedWorkers.Count - 1);
            }
            */
        }


        public override void Add(Agent agent)
        {
            base.Add(agent);
            unassignedWorkers.Add(agent);
        }

        public override Agent PopAt(int i)
        {
            Agent result = base.PopAt(i);
            for (int j = 0; j < unassignedWorkers.Count; j++)
            {
                if (unassignedWorkers[j] == result)
                {
                    unassignedWorkers.RemoveAt(j);
                    return result;
                }
            }
            foreach (BaseWorkers workers in baseWorkers)
            {
                for (int j = 0; j < workers.MineralWorkers.Count; j++)
                {
                    if (workers.MineralWorkers[j] == result)
                    {
                        workers.MineralWorkers.RemoveAt(j);
                        return result;
                    }
                }
            }
            return result;
        }
    }
}
