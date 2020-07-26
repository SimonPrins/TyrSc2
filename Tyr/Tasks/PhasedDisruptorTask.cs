using System.Collections.Generic;
using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    public class PhasedDisruptorTask : Task
    {
        public static PhasedDisruptorTask Task = new PhasedDisruptorTask();
        public Dictionary<ulong, int> PhasedFrame = new Dictionary<ulong, int>();

        public PhasedDisruptorTask() : base(15)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.DISRUPTOR_PHASED;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            HashSet<ulong> deadAgents = new HashSet<ulong>();
            foreach (ulong tag in PhasedFrame.Keys)
                deadAgents.Add(tag);
            foreach (Agent agent in Units)
            {
                deadAgents.Remove(agent.Unit.Tag);
                if (!PhasedFrame.ContainsKey(agent.Unit.Tag))
                    PhasedFrame.Add(agent.Unit.Tag, bot.Frame);
            }

            foreach (ulong deadAgentTag in deadAgents)
                PhasedFrame.Remove(deadAgentTag);
        }
    }
}
