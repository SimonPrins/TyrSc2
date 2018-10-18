using Tyr.Agents;

namespace Tyr.Tasks
{
    public class ProductionTask : Task
    {
        public static ProductionTask Task = new ProductionTask();

        public ProductionTask() : base(5)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsProductionStructure;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            foreach (Agent agent in units)
                if (agent.Unit.Orders.Count == 0)
                    tyr.Build.ProduceOverride(tyr, agent);
        }
    }
}
