using Tyr.Agents;

namespace Tyr.Tasks
{
    class HideLostWorkersTask : Task
    {
        public static HideLostWorkersTask Task = new HideLostWorkersTask();

        public HideLostWorkersTask() : base(5)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && Tyr.Bot.BaseManager.Main.ResourceCenter != null 
                && agent.Unit.Pos.Z < Tyr.Bot.BaseManager.Main.ResourceCenter.Unit.Pos.Z - 0.1
                && agent.DistanceSq(HideBaseTask.Task.HideLocation.BaseLocation.Pos) >= 20 * 20;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
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
