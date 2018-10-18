using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class HideBaseTask : Task
    {
        public static HideBaseTask Task = new HideBaseTask();
        public Base HideLocation;
        public bool BuildNexus = false;
        private bool NexusComplete = false;
        private bool ProbeSent = false;
        public int MoveOutFrame = 2240;
        public bool ClaimWorkersOutsideMain = false;
        public bool GoScout = false;

        public HideBaseTask() : base(8)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (ClaimWorkersOutsideMain && !Tyr.Bot.MapAnalyzer.MainAndPocketArea[SC2Util.To2D(agent.Unit.Pos)] && Tyr.Bot.MapAnalyzer.Placement[SC2Util.To2D(agent.Unit.Pos)]
                && agent.DistanceSq(HideLocation.BaseLocation.Pos) >= 20 * 20)
                return true;

            return !ProbeSent && agent.IsWorker && units.Count == 0 && Tyr.Bot.Frame >= MoveOutFrame;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = HideLocation.BaseLocation.Pos, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return HideLocation != null && !NexusComplete;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (units.Count > 0)
                ProbeSent = true;
            
            bool nexusBuilt = false;
            NexusComplete = false;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.NEXUS
                    && agent.DistanceSq(HideLocation.BaseLocation.Pos) <= 3 * 3)
                {
                    nexusBuilt = true;
                    if (agent.Unit.BuildProgress >= 0.99)
                        NexusComplete = true;
                    break;
                }

            if (NexusComplete)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
            {
                if (GoScout)
                {
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
                    if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BARRACKS) >= 3
                        || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REFINERY) > 0
                        || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) > 0
                        || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) > 0
                        || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.COMMAND_CENTER) >= 2)
                        GoScout = false;
                }
                else if (NexusComplete)
                {
                    if (HideLocation.BaseLocation.MineralFields.Count > 0 && (agent.Unit.Orders == null || agent.Unit.Orders.Count == 0))
                        agent.Order(Abilities.MOVE, HideLocation.BaseLocation.MineralFields[0].Tag);
                }
                else if (BuildNexus && !nexusBuilt)
                    agent.Order(BuildingType.LookUp[UnitTypes.NEXUS].Ability, HideLocation.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, HideLocation.BaseLocation.Pos);
            }
        }
    }
}
