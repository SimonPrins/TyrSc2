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
            Bot.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsProductionStructure;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
            {
                if (agent.Unit.Orders.Count == 0)
                    tyr.Build.ProduceOverride(tyr, agent);
                else if (agent.Unit.Orders.Count == 1
                    && tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag)
                    && (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.REACTOR
                        || tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.BARRACKS_REACTOR
                        || tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR
                        || tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_REACTOR))
                    tyr.Build.ProduceOverride(tyr, agent);
            }
        }
    }
}
