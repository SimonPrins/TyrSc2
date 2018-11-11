using Tyr.Agents;

namespace Tyr.Tasks
{
    public class SupplyDepotTask : Task
    {
        public static SupplyDepotTask Task = new SupplyDepotTask();
        public SupplyDepotTask() : base(1)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.SUPPLY_DEPOT;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            foreach (Agent agent in units)
                if (agent.Unit.UnitType == UnitTypes.SUPPLY_DEPOT)
                    agent.Order(556);
        }
    }
}
