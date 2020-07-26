using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class HideBuildingTask : Task
    {
        public static HideBuildingTask Task = new HideBuildingTask();
        public Base HideLocation;
        public int MoveOutFrame = 2240;
        private int CurrentOrder = 0;

        public List<uint> RequiredBuildings = new List<uint>();

        public HideBuildingTask() : base(20)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && units.Count == 0 && Bot.Main.Frame >= MoveOutFrame;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = HideLocation.BaseLocation.Pos, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return HideLocation != null;
        }

        public override void OnFrame(Bot bot)
        {
            if (CurrentOrder < RequiredBuildings.Count)
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == RequiredBuildings[CurrentOrder]
                        && agent.Unit.BuildProgress >= 0.99
                        && agent.DistanceSq(HideLocation.BaseLocation.Pos) <= 15 * 15)
                    {
                        CurrentOrder++;
                        break;
                    }
            }

            foreach (Agent agent in units)
            {
                if (CurrentOrder < RequiredBuildings.Count && agent.DistanceSq(HideLocation.BaseLocation.Pos) <= 15 * 15)
                {
                    int order = BuildingType.LookUp[RequiredBuildings[CurrentOrder]].Ability;
                    if (agent.Unit.Orders != null && agent.Unit.Orders.Count > 0 && agent.Unit.Orders[0].AbilityId == order)
                        continue;
                    if (bot.Frame % 4 == 0)
                    {
                        Point2D target = SC2Util.Point(HideLocation.BaseLocation.Pos.X, HideLocation.BaseLocation.Pos.Y - 1 + 3 * CurrentOrder);
                        agent.Order(order, target);
                    }
                }
                else
                    agent.Order(Abilities.MOVE, HideLocation.BaseLocation.Pos);
            }
        }
    }
}
