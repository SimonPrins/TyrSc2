using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    class HideLostWorkersTask : Task
    {
        public static HideLostWorkersTask Task = new HideLostWorkersTask();

        public HideLostWorkersTask() : base(5)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && Bot.Main.BaseManager.Main.ResourceCenter != null 
                && agent.Unit.Pos.Z < Bot.Main.BaseManager.Main.ResourceCenter.Unit.Pos.Z - 0.1
                && agent.DistanceSq(HideBaseTask.Task.HideLocation.BaseLocation.Pos) >= 20 * 20;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            for (int i = units.Count - 1; i >= 0; i--)
            {
                Agent agent = units[i];
                if (agent.DistanceSq(HideBaseTask.Task.HideLocation.BaseLocation.Pos) <= 20 * 20)
                {
                    IdleTask.Task.Add(agent);
                    units.RemoveAt(i);
                }
                else
                    agent.Order(Abilities.MOVE, HideBaseTask.Task.HideLocation.BaseLocation.Pos);
            }
        }
    }
}
