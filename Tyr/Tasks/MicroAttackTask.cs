using Tyr.Agents;

namespace Tyr.Tasks
{
    class MicroAttackTask : Task
    {
        public static MicroAttackTask Task = new MicroAttackTask();
        public int UnitType = -1;
        public MicroAttackTask() : base(5)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit || agent.IsWorker;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
                agent.Order(Abilities.ATTACK, tyr.TargetManager.AttackTarget);
        }
    }
}
