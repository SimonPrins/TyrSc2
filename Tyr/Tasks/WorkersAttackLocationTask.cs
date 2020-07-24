using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Tasks
{
    class WorkersAttackLocationTask : Task
    {
        public static WorkersAttackLocationTask Task = new WorkersAttackLocationTask();
        public Point2D AttackTarget = null;
        public int Max = 4;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public WorkersAttackLocationTask() : base(8)
        {}

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count < Max && AttackTarget != null)
                result.Add(new UnitDescriptor() { Count = Max - units.Count, UnitTypes = UnitTypes.WorkerTypes, Pos = AttackTarget });
            return result;
        }

        public override bool IsNeeded()
        {
            return AttackTarget != null;
        }

        public override void OnFrame(Bot tyr)
        {
            if (AttackTarget == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
                Attack(agent, AttackTarget);
        }
    }
}
