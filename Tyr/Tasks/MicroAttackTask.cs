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
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit || agent.IsWorker;
        }

        public override bool IsNeeded()
        {
            return true;
        }
        
        public override void OnFrame(Tyr tyr)
        {
            if (tyr.Frame == 20)
            {
                System.Console.WriteLine("Target: " + tyr.TargetManager.AttackTarget);
            }
            foreach (Agent agent in units)
                agent.Order(Abilities.ATTACK, tyr.TargetManager.AttackTarget);
        }
    }
}
