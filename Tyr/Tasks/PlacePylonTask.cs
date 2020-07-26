using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
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
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.PROBE && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Frame >= 400;
        }

        public override void OnFrame(Bot bot)
        {
            if (units.Count == 0)
                return;
            Agent probe = units[0];
            if (probe.Unit.Orders.Count > 0 &&
                    (probe.Unit.Orders[0].AbilityId == BuildingType.LookUp[UnitTypes.PYLON].Ability))
                return;

            if (bot.Frame < LastBuiltFrame + 5)
                return;

            if (bot.TargetManager.PotentialEnemyStartLocations.Count == 1 && SC2Util.DistanceSq(probe.Unit.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]) <= 20 * 20)
            {
                probe.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                return;
            }

            foreach (BuildingLocation building in bot.EnemyManager.EnemyBuildings.Values)
            {
                if (SC2Util.DistanceSq(probe.Unit.Pos, building.Pos) <= 8 * 8)
                {
                    probe.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                    
                    return;
                }
                if (SC2Util.DistanceSq(probe.Unit.Pos, building.Pos) <= 10 * 10)
                {
                    if (bot.Observation.Observation.PlayerCommon.Minerals < 100)
                    {
                        probe.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                        return;
                    }
                    else
                    {
                        Point2D buildLocation = bot.buildingPlacer.FindPlacement(SC2Util.To2D(probe.Unit.Pos), SC2Util.Point(2, 2), UnitTypes.PYLON);
                        probe.Order(BuildingType.LookUp[UnitTypes.PYLON].Ability, buildLocation);
                        LastBuiltFrame = bot.Frame;
                        return;
                    }
                }
            }
            probe.Order(Abilities.MOVE, bot.TargetManager.AttackTarget);
        }
    }
}
