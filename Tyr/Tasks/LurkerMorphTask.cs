using Tyr.Agents;
using Tyr.Util;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Tasks
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

        public override void OnFrame(Bot tyr)
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
