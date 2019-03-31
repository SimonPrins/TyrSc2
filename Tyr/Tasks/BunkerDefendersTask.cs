using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Tasks
{
    class BunkerDefendersTask : Task
    {
        public static BunkerDefendersTask Task = new BunkerDefendersTask();
        private Agent Bunker = null;
        public bool LeaveBunkers = false;
        private int BunkerDeterminedFrame = -1;

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public BunkerDefendersTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (GetBunker() == null)
                return false;
            return agent.Unit.UnitType == UnitTypes.MARINE && Units.Count < Tyr.Bot.Build.Count(UnitTypes.BUNKER) * 4;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();

            descriptors.Add(new UnitDescriptor() {
                Count = Tyr.Bot.Build.Completed(UnitTypes.BUNKER) * 4 - Units.Count,
                UnitTypes = new HashSet<uint>() { UnitTypes.MARINE }
            });

            return descriptors;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.Build.Completed(UnitTypes.BUNKER) > 0 && !LeaveBunkers;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (LeaveBunkers)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.BUNKER)
                        agent.Order(408);
                Clear();
                return;
            }
            GetBunker();
            if (Bunker == null)
            {
                Clear();
                return;
            }

            foreach (Agent agent in units)
                agent.Order(Abilities.MOVE, Bunker.Unit.Tag);
        }

        public Agent GetBunker()
        {
            if (Tyr.Bot.Frame == BunkerDeterminedFrame)
                return Bunker;
            BunkerDeterminedFrame = Tyr.Bot.Frame;

            Bunker = null;
            foreach (Agent bunker in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (bunker.Unit.UnitType != UnitTypes.BUNKER
                    || bunker.Unit.BuildProgress < 0.90)
                    continue;
                if (bunker.Unit.Passengers == null)
                    continue;

                if (bunker.Unit.Passengers.Count >= 4)
                    continue;
                Bunker = bunker;
                break;
            }
            return Bunker;
        }
    }
}
