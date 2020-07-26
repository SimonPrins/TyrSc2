using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    class HallucinationAttackTask : Task
    {
        public static HallucinationAttackTask Task = new HallucinationAttackTask();

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public HallucinationAttackTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.IsHallucination;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (Agent agent in units)
                Attack(agent, bot.TargetManager.AttackTarget);
        }
    }
}
