﻿using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Micro;

namespace SC2Sharp.Tasks
{
    public abstract class Task
    {
        protected List<Agent> units = new List<Agent>();

        private MicroController MicroController = new MicroController();
        public List<CustomController> BeforeControllers = new List<CustomController>();
        public List<CustomController> AfterControllers = new List<CustomController>();

        public bool AllowClaiming = true;
        public bool JoinCombatSimulation = false;

        public Task(int priority)
        {
            Priority = priority;
        }

        public void Cleanup(Bot bot)
        {
            // Remove units that are already dead.
            for (int i = units.Count - 1; i >= 0 ; i--)
                if (!bot.UnitManager.Agents.ContainsKey(units[i].Unit.Tag))
                    units.RemoveAt(i);
        }

        public abstract bool IsNeeded();
        public abstract void OnFrame(Bot bot);
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

        public void StopAndClear(bool stopped)
        {
            Stopped = stopped;
            if (Stopped)
                Clear();
        }

        public void ClearLast()
        {
            ClearAt(units.Count - 1);
        }

        public void ClearAt(int i)
        {
            IdleTask.Task.Add(units[i]);
            units[i] = units[units.Count - 1];
            units.RemoveAt(units.Count - 1);
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

        public static void Enable(Task task)
        {
            task.Stopped = false;
            Bot.Main.TaskManager.Add(task);
        }

        public void Attack(Agent agent, Point2D point)
        {
            if (MicroController.TryAttack(agent, point, BeforeControllers))
                return;
            if (Bot.Main.MicroController.TryAttack(agent, point))
                return;
            if (MicroController.TryAttack(agent, point, AfterControllers))
                return;
            agent.Order(Abilities.ATTACK, point);
        }

        public virtual void AddCombatSimulationUnits(List<Unit> simulationUnits)
        {
            if (JoinCombatSimulation)
                foreach (Agent agent in Units)
                    simulationUnits.Add(agent.Unit);
        }
    }
}
