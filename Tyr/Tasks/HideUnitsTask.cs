using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class HideUnitsTask : Task
    {
        public static HideUnitsTask Task = new HideUnitsTask();
        public Point2D Target;
        public uint UnitType = UnitTypes.BATTLECRUISER;

        public HideUnitsTask() : base(9)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitType;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
                if (SC2Util.DistanceSq(agent.Unit.Pos, Target) >= 5 * 5)
                    agent.Order(Abilities.MOVE, Target);
        }
    }
}
