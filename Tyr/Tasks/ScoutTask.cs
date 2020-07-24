using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Tasks
{
    class ScoutTask : Task
    {
        public static ScoutTask Task = new ScoutTask();
        public Point2D Target;
        public uint ScoutType;

        public ScoutTask() : base(11)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Target, Count = 1, UnitTypes = new HashSet<uint>() { ScoutType } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Target != null)
                foreach (Agent agent in units)
                    agent.Order(Abilities.MOVE, Target);
        }
    }
}
