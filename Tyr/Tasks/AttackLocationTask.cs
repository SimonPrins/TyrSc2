using SC2APIProtocol;
using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
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

        public override void OnFrame(Bot bot)
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
