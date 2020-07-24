using Tyr.Agents;

namespace Tyr.Tasks
{
    class ConstructingSCVsTask : Task
    {
        public static ConstructingSCVsTask Task = new ConstructingSCVsTask();
        public static void Enable()
        {
            Enable(Task);
        }

        public ConstructingSCVsTask() : base(2)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.SCV
                && agent.Unit.Orders != null
                && agent.Unit.Orders.Count > 0
                && BuildingType.BuildingAbilities.Contains((int)agent.Unit.Orders[0].AbilityId);
        }

        public override bool IsNeeded()
        {
            return true;
        }
        
        public override void OnFrame(Bot tyr)
        {
            for (int i = units.Count - 1; i >= 0; i--)
            {
                if (units[i].Unit.Orders == null
                    || units[i].Unit.Orders.Count == 0
                    || BuildingType.BuildingAbilities.Contains((int)units[i].Unit.Orders[0].AbilityId))
                {
                    IdleTask.Task.Add(units[i]);
                    RemoveAt(i);
                }
            }
        }
    }
}
