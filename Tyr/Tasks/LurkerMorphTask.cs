using SC2Sharp.Agents;
using SC2Sharp.Util;
using static SC2Sharp.Builds.BuildLists.ConditionalStep;

namespace SC2Sharp.Tasks
{
    public class LurkerMorphTask : Task
    {
        Test Condition;
        public LurkerMorphTask(Test condition) : base(12)
        {
            Condition = condition;
        }

        public override bool DoWant(Agent agent)
        {
            return Units.Count == 0 && Condition() && agent.Unit.UnitType == UnitTypes.HYDRALISK && SC2Util.DistanceGrid(agent.Unit.Pos, Bot.Main.MapAnalyzer.StartLocation) <= 30;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (Agent agent in units)
                agent.Order(Abilities.MORPH_LURKER);

            for (int i = units.Count - 1; i >= 0; i--)
            {
                if (units[i].Unit.UnitType != UnitTypes.HYDRALISK)
                {
                    IdleTask.Task.Add(units[i]);
                    units.RemoveAt(i);
                }
            }
        }
    }
}
