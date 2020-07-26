using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    public class ProductionTask : Task
    {
        public static ProductionTask Task = new ProductionTask();

        public ProductionTask() : base(5)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsProductionStructure;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (Agent agent in units)
            {
                if (agent.Unit.Orders.Count == 0)
                    bot.Build.ProduceOverride(bot, agent);
                else if (agent.Unit.Orders.Count == 1
                    && bot.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag)
                    && (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.REACTOR
                        || bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.BARRACKS_REACTOR
                        || bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR
                        || bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_REACTOR))
                    bot.Build.ProduceOverride(bot, agent);
            }
        }
    }
}
