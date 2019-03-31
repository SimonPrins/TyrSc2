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
        public abstract void OnStart(Tyr tyr);
        public abstract void OnFrame(Tyr tyr);
        public virtual void Produce(Tyr tyr, Agent agent) { }
        private Build PreviousOverrideBuild = null;

        protected List<CustomController> MicroControllers = new List<CustomController>();
        public List<CustomController> GetMicroControllers()
        {
            if (PreviousOverrideBuild != null)
                return PreviousOverrideBuild.MicroControllers;
            return MicroControllers;
        }

        public void ProduceOverride(Tyr tyr, Agent agent)
        {
            if (PreviousOverrideBuild != null)
                PreviousOverrideBuild.Produce(tyr, agent);
            else
                Produce(tyr, agent);
        }

        public void OnFrameBase(Tyr tyr)
        {
            Tyr.Bot.DrawText("Executing Build: " + Name());
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
            if (Tyr.Bot.MyRace == Race.Terran)
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
                return Tyr.Bot.BaseManager.Natural;
            }
        }

        public Base Main
        {
            get
            {
                return Tyr.Bot.BaseManager.Main;
            }
        }

        public Point2D NaturalDefensePos
        {
            get
            {
                return Tyr.Bot.BaseManager.NaturalDefensePos;
            }
        }

        public Point2D MainDefensePos
        {
            get
            {
                return Tyr.Bot.BaseManager.MainDefensePos;
            }
        }

        public int Minerals()
        {
            return (int)Tyr.Bot.Observation.Observation.PlayerCommon.Minerals - Tyr.Bot.ReservedMinerals;
        }

        public int Gas()
        {
            return (int)Tyr.Bot.Observation.Observation.PlayerCommon.Vespene - Tyr.Bot.ReservedGas;
        }

        public static uint FoodUsed()
        {
            return Tyr.Bot.Observation.Observation.PlayerCommon.FoodUsed;
        }

        public static uint AvailableFood()
        {
            return Tyr.Bot.Observation.Observation.PlayerCommon.FoodCap;
        }

        public static uint FoodLeft()
        {
            return AvailableFood() - FoodUsed();
        }

        public static uint ExpectedAvailableFood()
        {
            return Tyr.Bot.Observation.Observation.PlayerCommon.FoodCap + Tyr.Bot.UnitManager.FoodExpected;
        }

        public int Count(uint type)
        {
            return Tyr.Bot.UnitManager.Count(type);
        }

        public int Completed(uint type)
        {
            return Tyr.Bot.UnitManager.Completed(type);
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
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
                if (b.Owner == Tyr.Bot.PlayerId)
                    result += b.BaseLocation.MineralFields.Count;
            return result;
        }

        public static bool ConstructGas(uint unitType)
        {
            if (Tyr.Bot.Minerals() < BuildingType.LookUp[unitType].Minerals || Tyr.Bot.BaseManager.AvailableGasses == 0 || Tyr.Bot.Frame - gasConstructingFrame < 5)
                return false;
            
            foreach (Base loc in Tyr.Bot.BaseManager.Bases)
            {
                if (loc.Owner != Tyr.Bot.PlayerId)
                    continue;

                if (loc.ResourceCenter == null || loc.ResourceCenter.Unit.BuildProgress <= 0.7)
                    continue;

                foreach (Gas gas in loc.BaseLocation.Gasses)
                {
                    if (!gas.Available)
                        continue;

                    gasConstructingFrame = Tyr.Bot.Frame;
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

                    gasConstructingFrame = Tyr.Bot.Frame;
                    Construct(type, SC2Util.To2D(gas.Pos), b, gas);
                    return true;
                }
                return false;
            }
            Point2D buildLocation = Tyr.Bot.buildingPlacer.FindPlacement(b.BaseLocation.Pos, BuildingType.LookUp[type].Size, type);
            if (buildLocation == null)
                return false;
            Tyr.Bot.DrawText("Building " + UnitTypes.LookUp[type].Name + ".");
            ConstructionTask.Task.Build(type, b, buildLocation, null, false);
            return true;
        }

        public static bool Construct(uint type, Base b, Point2D pos, bool exact)
        {
            Point2D buildLocation;
            if (exact)
                buildLocation = pos;
            else
                buildLocation = Tyr.Bot.buildingPlacer.FindPlacement(pos, BuildingType.LookUp[type].Size, type, type == UnitTypes.SPINE_CRAWLER ? 5 : 15);
            
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
            return Construct(unitType, Tyr.Bot.BaseManager.Main);
        }

        public static bool ConstructResourceCenter(uint unitType)
        {
            // Check if there is already a Resource center constructing.
            foreach (Agent unit in Tyr.Bot.UnitManager.Agents.Values)
                if (unit.IsWorker && unit.Unit.Orders != null && unit.Unit.Orders.Count > 0 && unit.Unit.Orders[0].AbilityId == BuildingType.LookUp[unitType].Ability)
                    return false;

            Base picked = null;
            float dist = 1000000000;
            bool natural = Tyr.Bot.UnitManager.Count(unitType) == 1;
            
            foreach (Base loc in Tyr.Bot.BaseManager.Bases)
            {
                if (loc.Owner != -1)
                    continue;
                bool blocked = false;
                foreach (Unit enemy in Tyr.Bot.Enemies())
                {
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
                    BoolGrid creep = new ImageBoolGrid(Tyr.Bot.Observation.Observation.RawData.MapState.Creep, 1);
                    for (float dx = -2.5f; !blocked && dx <= 2.51f; dx++)
                        for (float dy = -2.5f; !blocked && dy <= 2.51f; dy++)
                            if (creep[(int)(loc.BaseLocation.Pos.X + dx), (int)(loc.BaseLocation.Pos.Y + dy)])
                                blocked = true;
                    if (blocked)
                        continue;
                }

                foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                {
                    if (!agent.IsBuilding)
                        continue;
                    blocked = System.Math.Abs(agent.Unit.Pos.X - loc.BaseLocation.Pos.X) < 5
                        && System.Math.Abs(agent.Unit.Pos.Y - loc.BaseLocation.Pos.Y) < 5;
                    if (blocked)
                        break;
                }
                if (blocked)
                {
                    File.AppendAllLines(Directory.GetCurrentDirectory() + "/Data/Tyr/debug.txt", new string[] { "Base at " + loc.BaseLocation.Pos + " blocked by allied unit." });
                    continue;
                }

                // Ignore the pocket expand as a first base.
                if (natural && Tyr.Bot.MapAnalyzer.EnemyDistances[(int)loc.BaseLocation.Pos.X, (int)loc.BaseLocation.Pos.Y] > Tyr.Bot.MapAnalyzer.EnemyDistances[(int)Tyr.Bot.MapAnalyzer.StartLocation.X, (int)Tyr.Bot.MapAnalyzer.StartLocation.Y])
                    continue;
                int newdist = loc.DistanceToMain;
                if (newdist < dist)
                {
                    dist = newdist;
                    picked = loc;
                }
            }
            if (picked == null)
            {
                ConstructionTask.Task.ExpandingBlockedUntilFrame = Tyr.Bot.Frame + 112;
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
    }
}
