using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.BuildingPlacement;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ProxyTask : Task
    {
        public static ProxyTask Task = new ProxyTask();
        public bool UseCloseHideLocation = true;
        public bool UseEnemyNatural = false;
        public Point2D HideLocation;

        public List<BuildRequest> BuildRequests = new List<BuildRequest>();

        public int DesiredWorkers = 1;

        public List<uint> RequiredBuildings = new List<uint>();
        public List<ProxyBuilding> Buildings;

        public bool EvadeEnemies = false;
        public Dictionary<uint, int> UnitCounts = new Dictionary<uint, int>();

        public ProxyTask() : base(20)
        { }

        public static void Enable(List<ProxyBuilding> buildings)
        {
            Task.Buildings = buildings;
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public static void Enable(List<ProxyBuilding> buildings, bool useCloseHideLocation)
        {
            Task.Buildings = buildings;
            Task.Stopped = false;
            Task.UseCloseHideLocation = useCloseHideLocation;
            Bot.Main.TaskManager.Add(Task);
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
            return Bot.Main.Frame > 100;
        }

        public Point2D GetHideLocation()
        {
            if (HideLocation == null)
            {
                if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count != 1)
                    return null;

                if (UseEnemyNatural)
                {
                    HideLocation = Bot.Main.MapAnalyzer.GetEnemyNatural().Pos;
                    return HideLocation;
                }

                Point2D enemyMain = Bot.Main.TargetManager.PotentialEnemyStartLocations[0];
                Point2D enemyNatural = Bot.Main.MapAnalyzer.GetEnemyNatural().Pos;
                Point2D closeTo = UseCloseHideLocation ? enemyMain : 
                        new PotentialHelper(enemyNatural, 30).From(enemyMain).Get();
                
                float dist = 10000;
                foreach (Base b in Bot.Main.BaseManager.Bases)
                {
                    float newDist = SC2Util.DistanceSq(closeTo, b.BaseLocation.Pos);
                    
                    if (newDist >= dist)
                        continue;

                    if (SC2Util.DistanceSq(enemyMain, b.BaseLocation.Pos) < 4)
                        continue;
                    if (SC2Util.DistanceSq(enemyNatural, b.BaseLocation.Pos) < 4)
                        continue;
                    dist = newDist;
                    HideLocation = b.BaseLocation.Pos;
                }
                
                HideLocation = new PotentialHelper(HideLocation, 6)
                    .To(Bot.Main.MapAnalyzer.StartLocation)
                    .Get();
            }
            return HideLocation;
        }

        public override void OnFrame(Bot bot)
        {
            Point2D hideLocation = GetHideLocation();
            if (hideLocation == null)
                return;
            UnitCounts = new Dictionary<uint, int>();
            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (agent.DistanceSq(HideLocation) > 20 * 20)
                    continue;

                CollectionUtil.Increment(UnitCounts, agent.Unit.UnitType);
            }
            DetermineNextBuilding(UnitCounts);
            List<BuildRequest> doneRequests = new List<BuildRequest>();
            foreach (BuildRequest request in BuildRequests)
            {
                if (request.worker != null && !Bot.Main.UnitManager.Agents.ContainsKey(request.worker.Unit.Tag))
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
                foreach (Agent agent in bot.UnitManager.Agents.Values)
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
            
            foreach (Agent agent in Units)
            {
                if (EvadeEnemies)
                {
                    float dist = 20 * 20;
                    Unit target = null;
                    foreach (Unit enemy in Bot.Main.Enemies())
                    {
                        if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                            continue;
                        if (!UnitTypes.CanAttackGround(enemy.UnitType))
                            continue;
                        float newDist = agent.DistanceSq(enemy);
                        if (newDist > dist)
                            continue;
                        dist = newDist;
                        target = enemy;
                    }
                    if (target != null)
                    {
                        Bot.Main.DrawLine(agent.Unit.Pos, target.Pos);
                        agent.Order(Abilities.MOVE, agent.From(target, 4));
                        continue;
                    }
                }

                bool building = false;
                foreach (BuildRequest request in BuildRequests)
                {
                    if (request.worker == null || request.worker.Unit.Tag != agent.Unit.Tag)
                        continue;
                    
                    foreach (Unit enemy in bot.Enemies())
                    {
                        if (!enemy.IsFlying && SC2Util.DistanceGrid(request.Pos, enemy.Pos) <= (UnitTypes.WorkerTypes.Contains(enemy.UnitType) ? 3 : 1))
                        {
                            Point2D placement = ProxyBuildingPlacer.FindPlacement(GetHideLocation(), BuildingType.LookUp[request.Type].Size, request.Type);
                            request.Pos = placement;
                        }
                    }

                    building = true;
                    if (agent.DistanceSq(request.Pos) <= 10 * 10)
                        agent.Order(BuildingType.LookUp[request.Type].Ability, request.Pos);
                    else
                        agent.Order(Abilities.MOVE, request.Pos);
                    break;
                }
                if (building)
                    continue;

                if (agent.DistanceSq(GetHideLocation()) >= 4 * 4)
                {
                    agent.Order(Abilities.MOVE, GetHideLocation());
                    continue;
                }
            }
        }

        public void DetermineNextBuilding(Dictionary<uint, int> unitCounts)
        {
            if (BuildRequests.Count > 0)
                return;
            Dictionary<uint, int> desiredUnitCounts = new Dictionary<uint, int>();
            foreach (ProxyBuilding building in Buildings)
            {
                if (!building.Test())
                    continue;
                CollectionUtil.Add(desiredUnitCounts, building.UnitType, building.Number);

                if (desiredUnitCounts[building.UnitType] > CollectionUtil.Get(unitCounts, building.UnitType))
                {
                    BuildingType buildingType = BuildingType.LookUp[building.UnitType];
                    Point2D placement = ProxyBuildingPlacer.FindPlacement(GetHideLocation(), buildingType.Size, building.UnitType);
                    if (placement != null)
                        BuildRequests.Add(new BuildRequest() { Type = building.UnitType, Pos = placement });
                    return;
                }
            }
        }
    }

    public class ProxyBuilding
    {
        public uint UnitType;
        public int Number = 1;
        public ConditionalStep.Test Test = () => true;
    }
}
