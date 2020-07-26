using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class OverlordAtNaturalTask : Task
    {
        public static OverlordAtNaturalTask Task = new OverlordAtNaturalTask();
        

        public OverlordAtNaturalTask() : base(7)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
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

        public override void OnFrame(Bot bot)
        {
            Point2D target;
            if (bot.BaseManager.Natural.Owner == bot.PlayerId)
                target = bot.BaseManager.NaturalDefensePos;
            else
                target = bot.BaseManager.MainDefensePos;

            foreach (Agent agent in units)
                if (SC2Util.DistanceSq(agent.Unit.Pos, target) >= 3 * 3)
                    agent.Order(Abilities.MOVE, target);
        }
    }
}
