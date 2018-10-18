using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Tasks
{
    class QueenInjectTask : Task
    {
        public static List<QueenInjectTask> Tasks = new List<QueenInjectTask>();
        private Base b;
        public QueenInjectTask(Base b) : base(10)
        {
            this.b = b;
        }

        public static void Enable()
        {
            if (Tasks.Count == 0)
                foreach (Base b in Tyr.Bot.BaseManager.Bases)
                {
                    QueenInjectTask queenInjectTask = new QueenInjectTask(b);
                    Tasks.Add(queenInjectTask);
                }

            foreach (Task task in Tasks)
            {
                task.Stopped = false;
                Tyr.Bot.TaskManager.Add(task);
            }
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = b.BaseLocation.Pos, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.QUEEN } });
            return result;
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.QUEEN && Units.Count == 0;
        }

        public override bool IsNeeded()
        {
            return b.Owner == Tyr.Bot.PlayerId;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (b.ResourceCenter == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
            {
                if (agent.DistanceSq(b.ResourceCenter) >= 7 * 7)
                    agent.Order(Abilities.MOVE, b.ResourceCenter.Unit.Tag);
                else
                    agent.Order(251, b.ResourceCenter.Unit.Tag);
            }
        }
    }
}
