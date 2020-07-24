using SC2APIProtocol;
using System.Collections.Generic;
using System.IO;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds
{
    public abstract class Build
    {
        public abstract string Name();
        public abstract void OnStart(Bot tyr);
        public abstract void OnFrame(Bot tyr);
        public virtual void Produce(Bot tyr, Agent agent) { }
        private Build PreviousOverrideBuild = null;

        protected List<CustomController> MicroControllers = new List<CustomController>();

        public static HashSet<uint> IgnoreExpansionBlockingUnitTypes = new HashSet<uint>();

        public List<CustomController> GetMicroControllers()
        {
            if (PreviousOverrideBuild != null)
                return PreviousOverrideBuild.MicroControllers;
            return MicroControllers;
        }

        public void ProduceOverride(Bot tyr, Agent agent)
        {
            if (PreviousOverrideBuild != null)
                PreviousOverrideBuild.Produce(tyr, agent);
            else
                Produce(tyr, agent);
        }

        public void OnFrameBase(Bot tyr)
        {
            Bot.Main.DrawText("Executing Build: " + Name());
            Build actualBuild = null;
            Build overrideBuild = this;
            while (overrideBuild != null)
            {
                actualBuild = overrideBuild;
                overrideBuild = overrideBuild.OverrideBuild();
            }

            if (PreviousOverrideBuild != null && PreviousOverrideBuild != actualBuild)
            {
                tyr.TaskManager.StopAll();
                actualBuild.InitializeTasks();
                tyr.TaskManager.ClearStopped();
            }
            actualBuild.OnFrame(tyr);
            actualBuild.Set.OnFrame();
            PreviousOverrideBuild = actualBuild;
        }

        public virtual void InitializeTasks()
        {
            IdleTask.Enable();
            ProductionTask.Enable();
            WorkerTask.Enable();
            ConstructionTask.Enable();
            MorphingTask.Enable();
            WorkerDefenseTask.Enable();
            if (Bot.Main.MyRace == Race.Terran)
                ConstructingSCVsTask.Enable();
            GasWorkerTask.Enable();
        }

        public virtual Build OverrideBuild() { return null; }

        public BuildSet Set = new BuildSet();

        private static int gasConstructingFrame = -100;

        public Base Natural
        {
            get
            {
                return Bot.Main.BaseManager.Natural;
            }
        }

        public Base Main
        {
            get
            {
                return Bot.Main.BaseManager.Main;
            }
        }

        public Point2D NaturalDefensePos
        {
            get
            {
                return Bot.Main.BaseManager.NaturalDefensePos;
            }
        }

        public Point2D MainDefensePos
        {
            get
            {
                return Bot.Main.BaseManager.MainDefensePos;
            }
        }

        public int Minerals()
        {
            return (int)Bot.Main.Observation.Observation.PlayerCommon.Minerals - Bot.Main.ReservedMinerals;
        }

        public int Gas()
        {
            return (int)Bot.Main.Observation.Observation.PlayerCommon.Vespene - Bot.Main.ReservedGas;
        }

        public static uint FoodUsed()
        {
            return Bot.Main.Observation.Observation.PlayerCommon.FoodUsed;
        }

        public static uint AvailableFood()
        {
            return Bot.Main.Observation.Observation.PlayerCommon.FoodCap;
        }

        public static uint FoodLeft()
        {
            return AvailableFood() - FoodUsed();
        }

        public static uint ExpectedAvailableFood()
        {
            return Bot.Main.Observation.Observation.PlayerCommon.FoodCap + Bot.Main.UnitManager.FoodExpected;
        }

        public int Count(uint type)
        {
            return Bot.Main.UnitManager.Count(type);
        }

        public int Completed(uint type)
        {
            return Bot.Main.UnitManager.Completed(type);
        }

        public int EnemyCount(uint type)
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(type);
        }

        public int TotalEnemyCount(uint type)
        {
            return Bot.Main.EnemyStrategyAnalyzer.TotalCount(type);
        }

        public static int Count(Base b, uint type)
        {
            if (b.BuildingCounts.ContainsKey(type))
                return b.BuildingCounts[type];
            else
                return 0;
        }

        public static int Completed(Base b, uint type)
        {
            if (b.BuildingsCompleted.ContainsKey(type))
                return b.BuildingsCompleted[type];
            else
                return 0;
        }

        public static int AvailableMineralPatches()
        {
            int result = 0;
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b.Owner == Bot.Main.PlayerId)
                    result += b.BaseLocation.MineralFields.Count;
            return result;
        }

        public static bool ConstructGas(uint unitType)
        {
            if (Bot.Main.Minerals() < BuildingType.LookUp[unitType].Minerals || Bot.Main.BaseManager.AvailableGasses == 0 || Bot.Main.Frame - gasConstructingFrame < 5)
                return false;
            
            foreach (Base loc in Bot.Main.BaseManager.Bases)
            {
                if (loc.Owner != Bot.Main.PlayerId)
                    continue;

                if (loc.ResourceCenter == null || loc.ResourceCenter.Unit.BuildProgress <= 0.7)
                    continue;

                foreach (Gas gas in loc.BaseLocation.Gasses)
                {
                    if (!gas.Available)
                        continue;

                    gasConstructingFrame = Bot.Main.Frame;
                    Construct(unitType, SC2Util.To2D(gas.Pos), loc, gas);
                    return true;
                }
            }
            return false;
        }

        public static void Construct(uint type, Point2D location, Base b, Point2D aroundLocation, bool exact)
        {
            ConstructionTask.Task.Build(type, b, location, aroundLocation, exact);
        }

        public static bool Construct(uint type, Base b)
        {
            if (UnitTypes.GasGeysers.Contains(type))
            {
                foreach (Gas gas in b.BaseLocation.Gasses)
                {
                    if (!gas.Available)
                        continue;

                    gasConstructingFrame = Bot.Main.Frame;
                    Construct(type, SC2Util.To2D(gas.Pos), b, gas);
                    return true;
                }
                return false;
            }
            Point2D buildLocation = Bot.Main.buildingPlacer.FindPlacement(b.BaseLocation.Pos, BuildingType.LookUp[type].Size, type);
            if (buildLocation == null)
                return false;
            Bot.Main.DrawText("Building " + UnitTypes.LookUp[type].Name + ".");
            ConstructionTask.Task.Build(type, b, buildLocation, null, false);
            return true;
        }

        public static bool Construct(uint type, Base b, Point2D pos, bool exact)
        {
            Point2D buildLocation;
            if (exact)
                buildLocation = pos;
            else
                buildLocation = Bot.Main.buildingPlacer.FindPlacement(pos, BuildingType.LookUp[type].Size, type, type == UnitTypes.SPINE_CRAWLER ? 5 : 15);
            
            if (buildLocation == null)
                return false;
            
            ConstructionTask.Task.Build(type, b, buildLocation, pos, exact);
            return true;
        }

        public static void Construct(uint type, Point2D location, Base b, Gas pickedGas)
        {
            ConstructionTask.Task.Build(type, b, location, pickedGas);
        }

        public static bool Construct(uint unitType)
        {
            if (UnitTypes.ResourceCenters.Contains(unitType))
                return ConstructResourceCenter(unitType);
            if (UnitTypes.GasGeysers.Contains(unitType))
                return ConstructGas(unitType);
            return Construct(unitType, Bot.Main.BaseManager.Main);
        }

        public static bool ConstructResourceCenter(uint unitType)
        {
            // Check if there is already a Resource center constructing.
            foreach (Agent unit in Bot.Main.UnitManager.Agents.Values)
                if (unit.IsWorker && unit.Unit.Orders != null && unit.Unit.Orders.Count > 0 && unit.Unit.Orders[0].AbilityId == BuildingType.LookUp[unitType].Ability)
                    return false;

            Base picked = null;
            float dist = 1000000000;
            bool natural = Bot.Main.UnitManager.Count(unitType) == 1;
            
            foreach (Base loc in Bot.Main.BaseManager.Bases)
            {
                if (loc.Owner != -1)
                    continue;
                bool blocked = false;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.IsFlying)
                        continue;
                    if (UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        continue;
                    if (IgnoreExpansionBlockingUnitTypes.Contains(enemy.UnitType))
                    continue;
                        if (SC2Util.DistanceSq(enemy.Pos, loc.BaseLocation.Pos) <= 6 * 6)
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked)
                    continue;

                if (unitType != UnitTypes.HATCHERY)
                {
                    // Check for creep.
                    BoolGrid creep = new ImageBoolGrid(Bot.Main.Observation.Observation.RawData.MapState.Creep, 1);
                    for (float dx = -2.5f; !blocked && dx <= 2.51f; dx++)
                        for (float dy = -2.5f; !blocked && dy <= 2.51f; dy++)
                            if (creep[(int)(loc.BaseLocation.Pos.X + dx), (int)(loc.BaseLocation.Pos.Y + dy)])
                                blocked = true;
                    if (blocked)
                        continue;
                }

                foreach (BuildingPlacement.ReservedBuilding reservedBuilding in Bot.Main.buildingPlacer.ReservedLocation)
                {
                    if (SC2Util.DistanceSq(reservedBuilding.Pos, loc.BaseLocation.Pos) <= 3 * 3)
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked)
                    continue;

                foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                {
                    if (!agent.IsBuilding)
                        continue;
                    blocked = !Bot.Main.buildingPlacer.CheckDistanceClose(loc.BaseLocation.Pos, unitType, SC2Util.To2D(agent.Unit.Pos), agent.Unit.UnitType);
                    //blocked = System.Math.Abs(agent.Unit.Pos.X - loc.BaseLocation.Pos.X) < 5
                    //    && System.Math.Abs(agent.Unit.Pos.Y - loc.BaseLocation.Pos.Y) < 5;
                    if (blocked)
                        break;
                }
                if (blocked)
                    continue;

                // Ignore the pocket expand as a first base.
                if (natural && Bot.Main.MapAnalyzer.EnemyDistances[(int)loc.BaseLocation.Pos.X, (int)loc.BaseLocation.Pos.Y] > Bot.Main.MapAnalyzer.EnemyDistances[(int)Bot.Main.MapAnalyzer.StartLocation.X, (int)Bot.Main.MapAnalyzer.StartLocation.Y])
                    continue;
                int newdist = loc.DistanceToMain - Bot.Main.MapAnalyzer.EnemyDistances[(int)loc.BaseLocation.Pos.X, (int)loc.BaseLocation.Pos.Y];
                if (newdist < dist)
                {
                    dist = newdist;
                    picked = loc;
                }
            }
            if (picked == null)
            {
                ConstructionTask.Task.ExpandingBlockedUntilFrame = Bot.Main.Frame + 112;
                return false;
            }
            
            ConstructionTask.Task.Build(unitType, picked, picked.BaseLocation.Pos, null, false);
            return true;
        }

        public void BalanceGas()
        {
            if (Minerals() >= 600)
                GasWorkerTask.WorkersPerGas = 3;
            else if (Gas() <= 300)
                GasWorkerTask.WorkersPerGas = 3;
            else if (Gas() >= 600)
                GasWorkerTask.WorkersPerGas = 1;
            else if (GasWorkerTask.WorkersPerGas >= 3)
            {
                if (Gas() >= 500)
                    GasWorkerTask.WorkersPerGas = 2;
            }
            else if (GasWorkerTask.WorkersPerGas == 2)
            {
                if (Gas() < 400)
                    GasWorkerTask.WorkersPerGas = 3;
            }
            else if (GasWorkerTask.WorkersPerGas <= 1)
            {
                if (Gas() < 500)
                    GasWorkerTask.WorkersPerGas = 2;
            }

        }

        public void CancelBuilding(uint unitType)
        {
            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == unitType
                    && agent.Unit.BuildProgress < 0.99)
                    agent.Order(Abilities.CANCEL);
            for (int i = ConstructionTask.Task.BuildRequests.Count - 1; i >= 0; i--)
            {
                BuildRequest request = ConstructionTask.Task.BuildRequests[i];
                if (request.Type != unitType)
                    continue;

                ConstructionTask.Task.BuildRequests.RemoveAt(i);
                if (ConstructionTask.Task.NaturalProbe != request.worker)
                {
                    IdleTask.Task.Add(request.worker);
                    ConstructionTask.Task.Units.Remove(request.worker);
                }
            }
            for (int i = ConstructionTask.Task.UnassignedRequests.Count - 1; i >= 0; i--)
            {
                BuildRequest request = ConstructionTask.Task.UnassignedRequests[i];
                if (request.Type != unitType)
                    continue;

                ConstructionTask.Task.UnassignedRequests.RemoveAt(i);
            }

        }
    }
}
