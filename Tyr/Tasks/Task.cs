using System;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Tasks
{
    public abstract class Task
    {
        protected List<Agent> units = new List<Agent>();

        public Task(int priority)
        {
            Priority = priority;
        }

        public void Cleanup(Tyr tyr)
        {
            // Remove units that are already dead.
            for (int i = units.Count - 1; i >= 0 ; i--)
                if (!tyr.UnitManager.Agents.ContainsKey(units[i].Unit.Tag))
                    units.RemoveAt(i);
        }

        public abstract bool IsNeeded();
        public abstract void OnFrame(Tyr tyr);
        public abstract bool DoWant(Agent agent);
        public int Priority { get; set; }
        public virtual List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor());
            return result;
        }

        public List<Agent> Units
        {
            get
            {
                return units;
            }
        }

        public bool Stopped { get; set; }

        public virtual void Add(Agent agent, UnitDescriptor descriptor)
        {
            units.Add(agent);
        }

        public virtual void Add(Agent agent)
        {
            units.Add(agent);
        }

        public void Clear(List<Agent> receiver)
        {
            foreach (Agent unit in units)
                receiver.Add(unit);
            units.Clear();
        }
        
        public void Clear(Task receiver)
        {
            foreach (Agent unit in units)
                receiver.Add(unit);
            units.Clear();
        }

        public void Clear()
        {
            Clear(IdleTask.Task);
        }

        public void RemoveAt(int i)
        {
            units[i] = units[units.Count - 1];
            units.RemoveAt(units.Count - 1);
        }

        public virtual Agent PopAt(int k)
        {
            Agent result = units[k];
            RemoveAt(k);
            return result;
        }
    }
}
