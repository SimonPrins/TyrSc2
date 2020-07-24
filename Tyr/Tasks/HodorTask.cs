using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Tasks
{
    /**
     * Hold the door!
     */
    public class HodorTask : Task
    {
        public static HodorTask Task = new HodorTask();
        public Point2D Target;
        public HashSet<uint> AllowedTypes = new HashSet<uint>() { UnitTypes.ADEPT, UnitTypes.ZEALOT };

        public HodorTask() : base(10)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Units.Count == 0)
                result.Add(new UnitDescriptor() { Count = 1, UnitTypes = AllowedTypes, Pos = Target });
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
            {
                tyr.DrawText("Hodor dist: " + System.Math.Sqrt(agent.DistanceSq(Target)));
                if (agent.DistanceSq(Target) <= 0.1)
                {
                    if (Bot.Main.Frame % 23 == 0)
                        agent.Order(18);
                }
                else
                    agent.Order(Abilities.ATTACK, Target);
            }
        }
    }
}
