using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class ConstructionTask : Task
    {
        public HashSet<ulong> BlockedWorkers = new HashSet<ulong>();
        public static ConstructionTask Task = new ConstructionTask();
        public List<BuildRequest> UnassignedRequests = new List<BuildRequest>();
        public List<BuildRequest> BuildRequests = new List<BuildRequest>();
        private List<Agent> unassignedAgents = new List<Agent>();
        public bool OnlyWorkersFromMain = false;
        public bool CancelBlockedBuildings = true;
        public bool OnlyCloseWorkers = true;
        public float MaxWorkerDist = 1000000;

        public int ExpandingBlockedUntilFrame = -100;

        public ConstructionTask() : base(10)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (!agent.IsWorker)
                return false;
            if (BlockedWorkers.Contains(agent.Unit.Tag))
                return false;
            if (OnlyCloseWorkers && SC2Util.DistanceGrid(agent.Unit.Pos, Tyr.Bot.MapAnalyzer.StartLocation) > 40
                && Tyr.Bot.Frame <= 3000)
                return false;
            if (OnlyWorkersFromMain && !Tyr.Bot.MapAnalyzer.StartArea[SC2Util.To2D(agent.Unit.Pos)])
                return false;
            if ((Tyr.Bot.MyRace == Race.Zerg || Tyr.Bot.MyRace == Race.Terran) && agent.Unit.Orders != null && agent.Unit.Orders.Count > 0 && Abilities.Creates.ContainsKey(agent.Unit.Orders[0].AbilityId))
                return false;
            if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                return false;
            return units.Count < UnassignedRequests.Count + BuildRequests.Count;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            foreach (BuildRequest request in UnassignedRequests)
                result.Add(new UnitDescriptor() { Pos = request.Pos, Count = 1, UnitTypes = UnitTypes.WorkerTypes, Marker = request, MaxDist = MaxWorkerDist });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            while (unassignedAgents.Count > 0 && UnassignedRequests.Count > 0)
            {
                UnassignedRequests[UnassignedRequests.Count - 1].worker = unassignedAgents[unassignedAgents.Count - 1];
                UnassignedRequests[UnassignedRequests.Count - 1].LastImprovementFrame = Tyr.Bot.Frame;
                UnassignedRequests[UnassignedRequests.Count - 1].Closest = unassignedAgents[unassignedAgents.Count - 1].DistanceSq(UnassignedRequests[UnassignedRequests.Count - 1].Pos);
                BuildRequests.Add(UnassignedRequests[UnassignedRequests.Count - 1]);
                UnassignedRequests.RemoveAt(UnassignedRequests.Count - 1);
                unassignedAgents.RemoveAt(unassignedAgents.Count - 1);
            }

            for (int i = BuildRequests.Count - 1; i >= 0; i--)
            {
                BuildRequest buildRequest = BuildRequests[i];
                bool completed = false;
                if (buildRequest.worker != null)
                {
                    float newDist = buildRequest.worker.DistanceSq(buildRequest.Pos);
                    if (newDist < buildRequest.Closest)
                    {
                        buildRequest.Closest = newDist;
                        buildRequest.LastImprovementFrame = Tyr.Bot.Frame;
                    }
                    else if (Tyr.Bot.Frame - buildRequest.LastImprovementFrame >= 448)
                    {
                        UnassignedRequests.Add(buildRequest);
                        BlockedWorkers.Add(buildRequest.worker.Unit.Tag);
                        BuildRequests[i] = BuildRequests[BuildRequests.Count - 1];
                        BuildRequests.RemoveAt(BuildRequests.Count - 1);
                        IdleTask.Task.Add(buildRequest.worker);
                        units.Remove(buildRequest.worker);
                    }
                    else if (UnitTypes.ResourceCenters.Contains(buildRequest.Type))
                    {
                        bool closeEnemy = false;
                        foreach (Unit enemy in Tyr.Bot.Enemies())
                        {
                            if (!UnitTypes.CanAttackGround(enemy.UnitType))
                                continue;
                            if (buildRequest.worker.DistanceSq(enemy) <= 8 * 8)
                            {
                                closeEnemy = true;
                                break;
                            }
                        }

                        if (closeEnemy)
                        {
                            ExpandingBlockedUntilFrame = Tyr.Bot.Frame + 224;
                            BuildRequests[i] = BuildRequests[BuildRequests.Count - 1];
                            BuildRequests.RemoveAt(BuildRequests.Count - 1);
                            IdleTask.Task.Add(buildRequest.worker);
                            units.Remove(buildRequest.worker);
                            DebugUtil.WriteLine("Base blocked, cancelling base. BuildRequest length: " + BuildRequests.Count);
                            continue;
                        }
                    }
                }
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (agent.Unit.UnitType == buildRequest.Type && SC2Util.DistanceSq(agent.Unit.Pos, buildRequest.Pos) <= 1)
                    {
                        completed = true;
                        agent.Base = buildRequest.Base;
                        agent.AroundLocation = buildRequest.AroundLocation;
                        agent.Exact = buildRequest.Exact;
                        break;
                    }
                }
                if (completed)
                {
                    BuildRequests[i] = BuildRequests[BuildRequests.Count - 1];
                    BuildRequests.RemoveAt(BuildRequests.Count - 1);
                    IdleTask.Task.Add(buildRequest.worker);
                    units.Remove(buildRequest.worker);
                }
                else if (!tyr.UnitManager.Agents.ContainsKey(buildRequest.worker.Unit.Tag))
                {
                    buildRequest.worker = null;
                    UnassignedRequests.Add(buildRequest);
                    BuildRequests[i] = BuildRequests[BuildRequests.Count - 1];
                    BuildRequests.RemoveAt(BuildRequests.Count - 1);
                } else if (CancelBlockedBuildings && Unbuildable(buildRequest))
                {
                    BuildRequests[i] = BuildRequests[BuildRequests.Count - 1];
                    BuildRequests.RemoveAt(BuildRequests.Count - 1);
                    IdleTask.Task.Add(buildRequest.worker);
                    units.Remove(buildRequest.worker);
                }
                else if (buildRequest.worker.Unit.Orders.Count == 0
                    || buildRequest.worker.Unit.Orders[0].AbilityId != BuildingType.LookUp[buildRequest.Type].Ability
                    || (buildRequest.worker.Unit.Orders[0].TargetWorldSpacePos != null && buildRequest.worker.Unit.Orders[0].TargetWorldSpacePos.X != buildRequest.Pos.X)
                    || (buildRequest.worker.Unit.Orders[0].TargetWorldSpacePos != null && buildRequest.worker.Unit.Orders[0].TargetWorldSpacePos.Y != buildRequest.Pos.Y)
                    || (buildRequest is BuildRequestGas && ((BuildRequestGas)buildRequest).Gas.Tag != buildRequest.worker.Unit.Orders[0].TargetUnitTag))
                {
                    Tyr.Bot.ReservedMinerals += BuildingType.LookUp[buildRequest.Type].Minerals;
                    Tyr.Bot.ReservedGas += BuildingType.LookUp[buildRequest.Type].Gas;

                    if (buildRequest is BuildRequestGas)
                        tyr.DrawLine(buildRequest.worker, ((BuildRequestGas)buildRequest).Gas.Pos);
                    else
                        tyr.DrawLine(buildRequest.worker, SC2Util.Point(buildRequest.Pos.X, buildRequest.Pos.Y, buildRequest.worker.Unit.Pos.Z));
                    if (tyr.Observation.Observation.PlayerCommon.Minerals < BuildingType.LookUp[buildRequest.Type].Minerals
                         || tyr.Observation.Observation.PlayerCommon.Vespene < BuildingType.LookUp[buildRequest.Type].Gas)
                    {
                        Point2D target = buildRequest is BuildRequestGas ? SC2Util.To2D(((BuildRequestGas)buildRequest).Gas.Pos) : buildRequest.Pos;
                        if (buildRequest.worker.DistanceSq(target) > 3 * 3)
                        {
                            buildRequest.worker.Order(Abilities.MOVE, target);
                            continue;
                        }
                    }

                    if (buildRequest is BuildRequestGas)
                    {
                        Unit gas = ((BuildRequestGas)buildRequest).Gas.Unit;
                        foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
                        {
                            if (SC2Util.DistanceSq(unit.Pos, ((BuildRequestGas)buildRequest).Gas.Pos) > 2 * 2)
                                continue;
                            gas = unit;
                            break;
                        }
                        buildRequest.worker.Order(BuildingType.LookUp[buildRequest.Type].Ability, gas.Tag);
                    }
                    else
                    {
                        buildRequest.worker.Order(BuildingType.LookUp[buildRequest.Type].Ability, buildRequest.Pos);
                    }
                }
            }

            foreach (BuildRequest request in UnassignedRequests)
                Tyr.Bot.DrawText("BuildRequest: " + UnitTypes.LookUp[request.Type].Name);
            foreach (BuildRequest request in BuildRequests)
                Tyr.Bot.DrawText("BuildRequest: " + UnitTypes.LookUp[request.Type].Name);
        }

        private bool Unbuildable(BuildRequest request)
        {
            if (UnitTypes.ResourceCenters.Contains(request.Type))
            {
                if (request.Type != UnitTypes.HATCHERY)
                {
                    // Check for creep.
                    BoolGrid creep = new ImageBoolGrid(Tyr.Bot.Observation.Observation.RawData.MapState.Creep, 1);
                    for (float dx = -2.5f; dx <= 2.51f; dx++)
                        for (float dy = -2.5f; dy <= 2.51f; dy++)
                            if (creep[(int)(request.Pos.X + dx), (int)(request.Pos.Y + dy)])
                                return true;
                }
                foreach (BuildingLocation loc in Tyr.Bot.EnemyManager.EnemyBuildings.Values)
                    if (SC2Util.DistanceSq(request.Pos, loc.Pos) <= 6 * 6)
                        return true;
                foreach (Unit enemy in Tyr.Bot.Enemies())
                    if (!enemy.IsFlying
                        && SC2Util.DistanceSq(request.Pos, enemy.Pos) <= 6 * 6)
                        return true;
                return false;
            }

            if (UnitTypes.GasGeysers.Contains(request.Type)
                || UnitTypes.ResourceCenters.Contains(request.Type)
                || UnitTypes.PYLON == request.Type)
                return false;

            if (Tyr.Bot.MyRace != Race.Zerg && request.Type != UnitTypes.COMMAND_CENTER && request.Type != UnitTypes.NEXUS)
            {
                Point2D size = BuildingType.LookUp[request.Type].Size;
                BoolGrid creep = new ImageBoolGrid(Tyr.Bot.Observation.Observation.RawData.MapState.Creep, 1);
                for (float dx = -size.X / 2f; dx <= size.X / 2f + 0.01f; dx++)
                    for (float dy = -size.Y / 2f; dy <= size.Y / 2f + 0.01f; dy++)
                        if (creep[(int)(request.Pos.X + dx), (int)(request.Pos.Y + dy)])
                            return true;
                
                foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                {
                    if (!agent.IsBuilding && agent.Unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
                        continue;
                    if (agent.DistanceSq(request.Pos) <= (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED ? (2 * 2) : 2))
                        return true;
                }

                foreach (BuildRequest existingRequest in UnassignedRequests)
                {
                    if (existingRequest == request)
                        continue;
                    if (SC2Util.DistanceSq(existingRequest.Pos, request.Pos) <= 2)
                        return true;
                }

                foreach (BuildRequest existingRequest in BuildRequests)
                {
                    if (existingRequest == request)
                        continue;
                    if (SC2Util.DistanceSq(existingRequest.Pos, request.Pos) <= 2)
                        return true;
                }
                return false;
            }

            return !Tyr.Bot.buildingPlacer.CheckPlacement(request.Pos, BuildingType.LookUp[request.Type].Size, request.Type, request, true);
        }

        public override void Add(Agent agent)
        {
            base.Add(agent);
            unassignedAgents.Add(agent);
        }

        public override void Add(Agent agent, UnitDescriptor descriptor)
        {
            base.Add(agent);
            BuildRequest request = (BuildRequest)descriptor.Marker;
            request.worker = agent;
            request.LastImprovementFrame = Tyr.Bot.Frame;
            request.Closest = agent.DistanceSq(request.Pos);
            BuildRequests.Add(request);
            UnassignedRequests.Remove(request);
        }

        public void Build(uint type, Base b, Point2D pos, Point2D aroundLocation, bool exact)
        {
            if (UnitTypes.ResourceCenters.Contains(type) && Tyr.Bot.Frame - ExpandingBlockedUntilFrame < 0)
                return;

            Tyr.Bot.ReservedMinerals += BuildingType.LookUp[type].Minerals;
            Tyr.Bot.ReservedGas += BuildingType.LookUp[type].Gas;
            if (type == UnitTypes.PYLON)
                Tyr.Bot.UnitManager.FoodExpected += 8;
            UnassignedRequests.Add(new BuildRequest() { Type = type, Base = b, Pos = pos, AroundLocation = aroundLocation, Exact = exact });
        }

        public void Build(uint type, Base b, Point2D pos, Gas gas)
        {
            foreach (BuildRequest request in UnassignedRequests)
                if (request is BuildRequestGas
                    && ((BuildRequestGas)request).Gas.Tag == gas.Tag)
                    return;
            foreach (BuildRequest request in BuildRequests)
                if (request is BuildRequestGas
                    && ((BuildRequestGas)request).Gas.Tag == gas.Tag)
                    return;

            Tyr.Bot.ReservedMinerals += BuildingType.LookUp[type].Minerals;
            Tyr.Bot.ReservedGas += BuildingType.LookUp[type].Gas;
            UnassignedRequests.Add(new BuildRequestGas() { Type = type, Base = b, Pos = pos, Gas = gas });
        }

    }
}
