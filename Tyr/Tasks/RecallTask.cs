using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Tasks
{
    class RecallTask : Task
    {
        public static RecallTask Task = new RecallTask();
        public Point2D Location = null;
        private int RecallFrame = -1;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public RecallTask() : base(10)
        {}

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();
            descriptors.Add(new UnitDescriptor() { Pos = Location, MaxDist = 20 });
            return descriptors;
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override bool IsNeeded()
        {
            return Location != null;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Location == null)
            {
                RecallFrame = -1;
                Clear();
                return;
            }
            if (RecallFrame == -1)
                RecallFrame = tyr.Frame;

            bool cleared = false;
            for (int i = Units.Count - 1; i >= 0; i--)
                if (Units[i].DistanceSq(Location) >= 20 * 20)
                {
                    cleared = true;
                    ClearAt(i);
                }

            if (cleared)
                Location = null;

            bool tooFar = false;
            foreach (Agent agent in units)
            {
                agent.Order(Abilities.MOVE, Location);
                if (agent.DistanceSq(Location) >= 2.5 * 2.5)
                    tooFar = true;
            }
            if (!tooFar || tyr.Frame - RecallFrame >= 22.4 * 10)
            {
                foreach (Agent agent in tyr.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.NEXUS)
                        continue;
                    if (agent.Unit.Energy < 50)
                        continue;
                    agent.Order(3686, Location);
                }
            }
        }
    }
}
