using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Tasks
{
    class AttackLocationTask : Task
    {
        public static AttackLocationTask Task = new AttackLocationTask();
        public Point2D AttackTarget = null;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public AttackLocationTask() : base(3)
        {}

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit;
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
