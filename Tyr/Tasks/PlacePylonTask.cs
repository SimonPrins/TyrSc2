using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class PlacePylonTask : Task
    {
        public static PlacePylonTask Task = new PlacePylonTask();
        public int UnitType = -1;
        public int LastBuiltFrame;

        public PlacePylonTask() : base(10)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.PROBE && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Tyr.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.Frame >= 400;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (units.Count == 0)
                return;
            Agent probe = units[0];
            if (probe.Unit.Orders.Count > 0 &&
                    (probe.Unit.Orders[0].AbilityId == BuildingType.LookUp[UnitTypes.PYLON].Ability))
                return;

            if (tyr.Frame < LastBuiltFrame + 5)
                return;

            if (tyr.TargetManager.PotentialEnemyStartLocations.Count == 1 && SC2Util.DistanceSq(probe.Unit.Pos, tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 20 * 20)
            {
                probe.Order(Abilities.MOVE, SC2Util.To2D(tyr.MapAnalyzer.StartLocation));
                return;
            }

            foreach (BuildingLocation building in tyr.EnemyManager.EnemyBuildings.Values)
            {
                if (SC2Util.DistanceSq(probe.Unit.Pos, building.Pos) <= 8 * 8)
                {
                    probe.Order(Abilities.MOVE, SC2Util.To2D(tyr.MapAnalyzer.StartLocation));
                    
                    return;
                }
                if (SC2Util.DistanceSq(probe.Unit.Pos, building.Pos) <= 10 * 10)
                {
                    if (tyr.Observation.Observation.PlayerCommon.Minerals < 100)
                    {
                        probe.Order(Abilities.MOVE, SC2Util.To2D(tyr.MapAnalyzer.StartLocation));
                        return;
                    }
                    else
                    {
                        Point2D buildLocation = tyr.buildingPlacer.FindPlacement(SC2Util.To2D(probe.Unit.Pos), SC2Util.Point(2, 2), UnitTypes.PYLON);
                        probe.Order(BuildingType.LookUp[UnitTypes.PYLON].Ability, buildLocation);
                        LastBuiltFrame = tyr.Frame;
                        return;
                    }
                }
            }
            probe.Order(Abilities.MOVE, tyr.TargetManager.AttackTarget);
        }
    }
}
