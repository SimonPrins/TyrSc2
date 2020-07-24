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
            Bot.Main.TaskManager.Add(Task);
        }

        public BunkerDefendersTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (GetBunker() == null)
                return false;
            return agent.Unit.UnitType == UnitTypes.MARINE && Units.Count < Bot.Main.Build.Count(UnitTypes.BUNKER) * 4;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();

            descriptors.Add(new UnitDescriptor() {
                Count = Bot.Main.Build.Completed(UnitTypes.BUNKER) * 4 - Units.Count,
                UnitTypes = new HashSet<uint>() { UnitTypes.MARINE }
            });

            return descriptors;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Build.Completed(UnitTypes.BUNKER) > 0 && !LeaveBunkers;
        }

        public override void OnFrame(Bot tyr)
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
            if (Bot.Main.Frame == BunkerDeterminedFrame)
                return Bunker;
            BunkerDeterminedFrame = Bot.Main.Frame;

            Bunker = null;
            foreach (Agent bunker in Bot.Main.UnitManager.Agents.Values)
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
