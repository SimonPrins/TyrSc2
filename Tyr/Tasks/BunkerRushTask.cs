using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class BunkerRushTask : Task
    {
        public static BunkerRushTask Task = new BunkerRushTask();
        public Point2D HideLocation;

        public List<BuildRequest> BuildRequests = new List<BuildRequest>();

        public int DesiredWorkers = 3;

        public List<uint> RequiredBuildings = new List<uint>();

        public BunkerRushTask() : base(20)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (DesiredWorkers - Units.Count > 0 && GetHideLocation() != null)
                result.Add(new UnitDescriptor() { Pos = GetHideLocation(), Count = DesiredWorkers - Units.Count, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public Point2D GetHideLocation()
        {
            if (HideLocation == null)
            {
                if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
                    return null;

                Point2D enemyMain = Bot.Bot.TargetManager.PotentialEnemyStartLocations[0];
                Point2D enemyNatural = Bot.Bot.MapAnalyzer.GetEnemyNatural().Pos;

                Point2D enemyThird = null;
                float dist = 10000;
                foreach (Base b in Bot.Bot.BaseManager.Bases)
                {
                    float newDist = SC2Util.DistanceSq(enemyMain, b.BaseLocation.Pos);

                    if (newDist < 4)
                        continue;
                    if (newDist >= dist)
                        continue;

                    if (SC2Util.DistanceSq(enemyNatural, b.BaseLocation.Pos) < 4)
                        continue;
                    dist = newDist;
                    enemyThird = b.BaseLocation.Pos;
                }
                HideLocation = enemyThird;
            }
            return HideLocation;
        }

        public override void OnFrame(Bot tyr)
        {
            BuildingType barracksType = BuildingType.LookUp[UnitTypes.BARRACKS];
            if (Bot.Bot.UnitManager.Count(UnitTypes.BARRACKS) < 2 && tyr.Minerals() >= 150 && BuildRequests.Count == 0)
            {
                Point2D placement = ProxyBuildingPlacer.FindPlacement(GetHideLocation(), barracksType.Size, UnitTypes.BARRACKS);
                BuildRequests.Add(new BuildRequest() { Type = UnitTypes.BARRACKS, Pos = placement });
            }
            else if (Bot.Bot.UnitManager.Count(UnitTypes.BUNKER) < 2 && tyr.Minerals() >= 100 && BuildRequests.Count == 0 && tyr.UnitManager.Completed(UnitTypes.BARRACKS) > 0 && tyr.UnitManager.Count(UnitTypes.BARRACKS) >= 2)
            {
                PotentialHelper helper = new PotentialHelper(tyr.MapAnalyzer.GetEnemyNatural().Pos);
                helper.Magnitude = 4;
                helper.From(tyr.MapAnalyzer.GetEnemyRamp(), 1);
                Point2D placement = ProxyBuildingPlacer.FindPlacement(helper.Get(), barracksType.Size, UnitTypes.BUNKER);
                BuildRequests.Add(new BuildRequest() { Type = UnitTypes.BUNKER, Pos = placement });
            }
            else if (Bot.Bot.UnitManager.Count(UnitTypes.BUNKER) >= 2 && tyr.Minerals() >= 100 && BuildRequests.Count == 0 && tyr.UnitManager.Count(UnitTypes.SIEGE_TANK) >= 2 && tyr.UnitManager.Count(UnitTypes.BARRACKS) >= 2 && tyr.UnitManager.Completed(UnitTypes.ENGINEERING_BAY) >= 1 && tyr.UnitManager.Count(UnitTypes.MISSILE_TURRET) < 2)
            {
                PotentialHelper helper = new PotentialHelper(tyr.MapAnalyzer.GetEnemyNatural().Pos);
                helper.Magnitude = 4;
                helper.From(tyr.MapAnalyzer.GetEnemyRamp(), 1);
                Point2D placement = ProxyBuildingPlacer.FindPlacement(helper.Get(), new Point2D() { X = 2, Y = 2}, UnitTypes.MISSILE_TURRET);
                BuildRequests.Add(new BuildRequest() { Type = UnitTypes.MISSILE_TURRET, Pos = placement });
            }

            List<BuildRequest> doneRequests = new List<BuildRequest>();
            foreach (BuildRequest request in BuildRequests)
            {
                if (request.worker != null && !Bot.Bot.UnitManager.Agents.ContainsKey(request.worker.Unit.Tag))
                    request.worker = null;
                if (request.worker == null)
                {
                    foreach(Agent agent in Units)
                    {
                        if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                            continue;
                        request.worker = agent;
                        break;
                    }
                }

                if (!ProxyBuildingPlacer.CheckPlacement(request.Pos, BuildingType.LookUp[request.Type].Size, request.Type, null, true))
                {
                    doneRequests.Add(request);
                    continue;
                }
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (agent.Unit.UnitType == request.Type
                        && agent.DistanceSq(request.Pos) < 4)
                    {
                        doneRequests.Add(request);
                        break;
                    }
                }
            }

            foreach (BuildRequest request in doneRequests)
                BuildRequests.Remove(request);

            Agent bunker = null;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.BUNKER)
                    continue;
                if (agent.Unit.BuildProgress < 0.99)
                    continue;
                if (bunker == null || agent.Unit.Health < agent.Unit.HealthMax)
                    bunker = agent;
            }
            Agent bc = null;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.BATTLECRUISER)
                    continue;
                if (bunker == null || agent.DistanceSq(bunker.Unit.Pos) >= 5 * 5)
                    continue;
                if (agent.Unit.Health < agent.Unit.HealthMax)
                    bc = agent;
            }

            foreach (Agent agent in Units)
            {
                bool building = false;
                foreach (BuildRequest request in BuildRequests)
                {
                    if (request.worker == null || request.worker.Unit.Tag != agent.Unit.Tag)
                        continue;

                    building = true;
                    agent.Order(BuildingType.LookUp[request.Type].Ability, request.Pos);
                    break;
                }
                if (building)
                    continue;

                if (bunker != null)
                {
                    if (bunker.Unit.Health < bunker.Unit.HealthMax)
                        agent.Order(Abilities.REPAIR, bunker.Unit.Tag);
                    else if (bc != null)
                        agent.Order(Abilities.REPAIR, bc.Unit.Tag);
                    else
                        agent.Order(Abilities.MOVE, bunker.From(tyr.TargetManager.PotentialEnemyStartLocations[0], 3));
                    continue;
                }

                if (agent.DistanceSq(GetHideLocation()) >= 4 * 4)
                {
                    agent.Order(Abilities.MOVE, GetHideLocation());
                    continue;
                }
            }
        }
    }
}
