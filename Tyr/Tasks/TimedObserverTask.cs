using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Tasks
{
    class TimedObserverTask : Task
    {
        public static TimedObserverTask Task = new TimedObserverTask();

        public Point2D Target;
        public TimedObserverTask() : base(11)
        { }

        public static void Enable()
        {
            Enable(Task);
        }
            

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.OBSERVER && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Tyr.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OBSERVER } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (Target != null)
                foreach (Agent agent in units)
                    agent.Order(Abilities.MOVE, Target);
        }
    }
}
