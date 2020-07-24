using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class NinjaTurtleCarrier : Build
    {
        private bool DefendRush = false;
        private bool LiftingDetected = false;
        private bool ChasingLiftedBuildings = false;
        public bool SkipDefenses = false;
        public int RequiredSize = 8;
        private bool DefendReapers = false;
        private bool DefendMarines = false;
        private FearVikingsController FearVikingsController = new FearVikingsController() { Stopped = true };
        private Base FarBase;
        public bool BuildCarriers = true;

        public override string Name()
        {
            return "NinjaTurtleCarrier";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            
            HideBaseTask.Enable();
            HideLostWorkersTask.Enable();
            
            FlyerDestroyTask.Enable();
            if (Bot.Main.EnemyRace == Race.Protoss)
                ProxySpotterTask.Enable();
            ShieldBatteryTargetTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            tyr.Monday = true;

            MicroControllers.Add(FearVikingsController);
            MicroControllers.Add(new CarrierController());
            MicroControllers.Add(new StutterController());

            double distance = 0;
            foreach (Base b in tyr.BaseManager.Bases)
            {
                double newDist = Math.Sqrt(SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.BaseManager.Main.BaseLocation.Pos)) + Math.Sqrt(SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.TargetManager.PotentialEnemyStartLocations[0]));
                
                if (newDist > distance)
                {
                    FarBase = b;
                    distance = newDist;
                }
            }
            HideBaseTask.Task.HideLocation = FarBase;
            
            Set += BuildWallPylon();
            Set += ProtossBuildUtil.Pylons();
            Set += HiddenBaseBuild();
            Set += MainBuild();
        }

        private BuildList BuildWallPylon()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building3, true);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            Point2D cannon1Pos = SC2Util.Point(Bot.Main.MapAnalyzer.building1.X + (Bot.Main.MapAnalyzer.building1.X - Bot.Main.MapAnalyzer.building2.X) / 2, Bot.Main.MapAnalyzer.building1.Y + (Bot.Main.MapAnalyzer.building1.Y - Bot.Main.MapAnalyzer.building2.Y) / 2);
            Point2D cannon2Pos = SC2Util.Point(Bot.Main.MapAnalyzer.building2.X + (Bot.Main.MapAnalyzer.building2.X - Bot.Main.MapAnalyzer.building1.X) / 2, Bot.Main.MapAnalyzer.building2.Y + (Bot.Main.MapAnalyzer.building2.Y - Bot.Main.MapAnalyzer.building1.Y) / 2);

            List<Point2D> gasses = new List<Point2D>();
            foreach (Unit unit in Bot.Main.Observation.Observation.RawData.Units)
                if (UnitTypes.GasGeysers.Contains(unit.UnitType) && SC2Util.DistanceSq(unit.Pos, Bot.Main.MapAnalyzer.StartLocation) <= 20 * 20)
                    gasses.Add(SC2Util.To2D(unit.Pos));

            result.Building(UnitTypes.FORGE, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building1, true);
            result.Building(UnitTypes.GATEWAY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building2, true);
            result.Building(UnitTypes.PYLON, Main);
            result.Building(UnitTypes.PYLON, Main, gasses[0], () => { return !DefendMarines; });
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, cannon1Pos);
            result.Building(UnitTypes.PYLON, Main, gasses[1], () => { return !DefendMarines; });
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, cannon2Pos);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, gasses[0], () => { return !DefendMarines; });
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, gasses[1], () => { return !DefendMarines; });
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.SHIELD_BATTERY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building1, () => { return DefendReapers || DefendMarines; });
            result.Building(UnitTypes.PYLON, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building1,
                () => { return DefendMarines; });
            result.Building(UnitTypes.PYLON, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building2,
                () => { return DefendMarines; });
            result.Building(UnitTypes.SHIELD_BATTERY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building2, () => { return DefendReapers || DefendMarines; });
            //result.Building(UnitTypes.PHOTON_CANNON, Tyr.Bot.BaseManager.Main, Tyr.Bot.MapAnalyzer.building3);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, gasses[0], () => { return DefendReapers; });
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, gasses[1], () => { return DefendReapers; });
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.STARGATE, 2);

            return result;
        }

        private BuildList HiddenBaseBuild()
        {
            BuildList result = new BuildList();

            result.If(() =>
            { return FarBase.ResourceCenter != null && FarBase.ResourceCenter.Unit.BuildProgress >= 0.99; });
            result.Building(UnitTypes.ASSIMILATOR, FarBase, 2);
            /*
            result.If(() => { return Tyr.Bot.Minerals() >= 1000; });
            result.Building(UnitTypes.PYLON, FarBase);
            result.Goto(-2);
            */
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            DefendMarines = tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BARRACKS) >= 3
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REFINERY) == 0
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) == 0
                && false;

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3 && tyr.Frame <= 210 * 22.4)
            {
                DefendReapers = true;
                RequiredSize = 6;
            }

            if (RequiredSize > 6)
            {
                if (Completed(UnitTypes.CARRIER) >= 12)
                    RequiredSize = 6;

                foreach (Unit enemy in tyr.Enemies())
                    if ((enemy.UnitType == UnitTypes.MARAUDER
                        || enemy.UnitType == UnitTypes.MARAUDER)
                        && SC2Util.DistanceSq(tyr.MapAnalyzer.StartLocation, enemy.Pos) <= 40 * 40)
                    {
                        RequiredSize = 6;
                        break;
                    }
            }

            foreach (Task task in WorkerDefenseTask.Tasks)
                task.Stopped = true;
            tyr.buildingPlacer.BuildCompact = true;
            WorkerTask.Task.StopTransfers = true;
            HideBaseTask.Task.MoveOutFrame = 672;
            ConstructionTask.Task.CancelBlockedBuildings = false;
            ConstructionTask.Task.OnlyCloseWorkers = false;
            ConstructionTask.Task.MaxWorkerDist = 30;

            tyr.NexusAbilityManager.PriotitizedAbilities.Add(948);
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(950);

            tyr.DrawText("Required size: " + RequiredSize);
            FlyerAttackTask.Task.RequiredSize = RequiredSize;
            FlyerDestroyTask.Task.RequiredSize = RequiredSize;
            IdleTask.Task.FearEnemies = true;

            DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.VIKING_FIGHTER);
            
            if (Count(UnitTypes.PROBE) <= 6)
                GasWorkerTask.WorkersPerGas = 0;
            else if (Count(UnitTypes.PROBE) <= 12)
                GasWorkerTask.WorkersPerGas = 1;
            else if (Count(UnitTypes.PROBE) <= 18)
                GasWorkerTask.WorkersPerGas = 2;
            else
                GasWorkerTask.WorkersPerGas = 3;

            if (tyr.Frame == 118)
                tyr.Chat("Time for some monobattles!");
            if (tyr.Frame == 163)
            {
                if (BuildCarriers)
                    tyr.Chat("I choose Carriers! :D");
                else
                    tyr.Chat("I choose Skillrays! :D");
            }

            if (!LiftingDetected)
                foreach (Unit unit in tyr.Enemies())
                    if (unit.IsFlying && UnitTypes.BuildingTypes.Contains(unit.UnitType))
                        LiftingDetected = true;

            if(LiftingDetected && tyr.EnemyManager.EnemyBuildings.Count == 0 && !ChasingLiftedBuildings)
            {
                ChasingLiftedBuildings = true;
                tyr.TaskManager.Add(new ElevatorChaserTask());
            }
                

            if (!DefendRush && tyr.Frame <= 4800 && Bot.Main.EnemyRace != Race.Zerg)
            {
                int enemyCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                    if (SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 40 * 40)
                        enemyCount++;

                if (enemyCount >= 3)
                    DefendRush = true;
            }

            if (Count(UnitTypes.STARGATE) > 0)
                HideBaseTask.Task.BuildNexus = true;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && agent.Unit.AssignedHarvesters < 16
                && Count(UnitTypes.PROBE) < 40)
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            if (agent.Unit.UnitType == UnitTypes.STARGATE)
            {
                if (Minerals() >= 250
                    && Gas() >= 150
                    && FoodUsed() + 4 <= 200
                    && !BuildCarriers)
                    agent.Order(950);
                else if (Minerals() >= 350
                    && Gas() >= 250
                    && FoodUsed() + 8 <= 200
                    && BuildCarriers)
                    agent.Order(948);
            }
            if (agent.Unit.UnitType == UnitTypes.FLEET_BEACON)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && BuildCarriers
                    && Count(UnitTypes.CARRIER) >= 3)
                    agent.Order(44);
            }
            if (agent.Unit.UnitType == UnitTypes.CYBERNETICS_CORE)
            {
                if (Count(UnitTypes.VOID_RAY) + Count(UnitTypes.CARRIER) >= 3)
                {
                    if (!tyr.Observation.Observation.RawData.Player.UpgradeIds.Contains(78))
                        agent.Order(1562);
                    else if (!tyr.Observation.Observation.RawData.Player.UpgradeIds.Contains(81))
                        agent.Order(1565);
                    else if (!tyr.Observation.Observation.RawData.Player.UpgradeIds.Contains(79))
                        agent.Order(1563);
                    else if (!tyr.Observation.Observation.RawData.Player.UpgradeIds.Contains(82))
                        agent.Order(1566);
                    else if (!tyr.Observation.Observation.RawData.Player.UpgradeIds.Contains(80))
                        agent.Order(1564);
                    else if (!tyr.Observation.Observation.RawData.Player.UpgradeIds.Contains(83))
                        agent.Order(1567);
                }
            }
        }
    }
}
