using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    class OverlordAtNaturalTask : Task
    {
        public static OverlordAtNaturalTask Task = new OverlordAtNaturalTask();
        

        public OverlordAtNaturalTask() : base(7)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.OVERLORD && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OVERLORD } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            Point2D target;
            if (tyr.BaseManager.Natural.Owner == tyr.PlayerId)
                target = tyr.BaseManager.NaturalDefensePos;
            else
                target = tyr.BaseManager.MainDefensePos;

            foreach (Agent agent in units)
                if (SC2Util.DistanceSq(agent.Unit.Pos, target) >= 3 * 3)
                    agent.Order(Abilities.MOVE, target);
        }
    }
}
