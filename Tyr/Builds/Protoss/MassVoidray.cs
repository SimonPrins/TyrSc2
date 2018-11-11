using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class MassVoidray : Build
    {
        private bool DefendRush = false;
        private BuildingStep ReaperDefenseCannonStep;
        private BuildingStep NaturalMirrorPylonStep;
        private BuildingStep NaturalMirrorCannonStep;
        private bool LiftingDetected = false;
        private bool ChasingLiftedBuildings = false;
        public bool SkipDefenses = false;
        public int RequiredSize = 14;
        private bool DefendReapers = false;
        private FearVikingsController FearVikingsController = new FearVikingsController() { Stopped = true };
        private Base FarBase;
        public bool BuildCarriers = false;
        private bool Beyond2Cannons = true;

        public override string Name()
        {
            return "MassVoidray";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            FlyerAttackTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
            WorkerSafetyTask.Enable();

            if (Tyr.Bot.EnemyRace == Race.Terran)
                HideBaseTask.Enable();
            else if (Tyr.Bot.EnemyRace == Race.Protoss)
                WorkerScoutTask.Enable();

            FlyerDestroyTask.Enable();
            if (Tyr.Bot.EnemyRace == Race.Protoss)
                ProxySpotterTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            tyr.Monday = true;

            MicroControllers.Add(FearVikingsController);
            MicroControllers.Add(new VoidrayController());
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

            if (SkipDefenses)
                Set += ProtossBuildUtil.Pylons();
            else
            {
                Set += NaturalDefenses();
                Set += RushDefenses();
                Set += ProtossBuildUtil.Pylons();
                Set += BuildReaperDefenseCannon();
                Set += BuildReaperRushDefense();
                if (Tyr.Bot.EnemyRace != Race.Zerg)
                Set += ProtossBuildUtil.Nexus(2);
            }
            Set += PowerPylons();
            Set += MainBuild();
        }

        private BuildList NaturalDefenses()
        {
            BuildList result = new BuildList();

            result.If(() => { return Tyr.Bot.EnemyRace != Race.Zerg || Count(UnitTypes.GATEWAY) > 0; });
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.FORGE, Natural, NaturalDefensePos);
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2);
            result.If(() => { return Count(UnitTypes.NEXUS) >= 2 && Beyond2Cannons; });
            if (Tyr.Bot.EnemyRace == Race.Zerg)
                result.If(() => { return Count(UnitTypes.CYBERNETICS_CORE) > 0; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, () => { return !DefendReapers; });
            if (Tyr.Bot.EnemyRace == Race.Zerg)
                result.If(() => { return Count(UnitTypes.STARGATE) > 0; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, () => { return !DefendReapers; });
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos);
            result.If(() => { return !DefendReapers; });
            result.If(() => { return Minerals() >= 650 && Tyr.Bot.Frame % 9 == 0; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2);
            result.If(() => { return Minerals() >= 650; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2);
            result.If(() => { return Minerals() >= 650; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2);

            return result;
        }

        private BuildList RushDefenses()
        {
            BuildList result = new BuildList();

            result.If(() => { return Count(UnitTypes.FORGE) > 0 && DefendRush; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 4);
            result.If(() => { return !DefendReapers; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2);

            return result;
        }

        private BuildList BuildReaperDefenseCannon()
        {
            BuildList result = new BuildList();

            Base reaperBase = null;
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
                if (b != Tyr.Bot.BaseManager.Main && b != Tyr.Bot.BaseManager.Natural)
                    reaperBase = b;

            Base naturalMirrorBase = null;
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
                if (b != Tyr.Bot.BaseManager.Main && b != Tyr.Bot.BaseManager.Natural
                    && b != reaperBase)
                    naturalMirrorBase = b;

            ReaperDefenseCannonStep = new BuildingStep(UnitTypes.PHOTON_CANNON, reaperBase);
            NaturalMirrorPylonStep = new BuildingStep(UnitTypes.PYLON, naturalMirrorBase);
            NaturalMirrorCannonStep = new BuildingStep(UnitTypes.PHOTON_CANNON, naturalMirrorBase);

            result.If(() => { return ReaperDefenseCannonStep.DesiredPos != null; });
            result += ReaperDefenseCannonStep;
            result.If(() => { return NaturalMirrorCannonStep.DesiredPos != null; });
            result += NaturalMirrorPylonStep;
            result += NaturalMirrorCannonStep;

            return result;
        }

        private BuildList BuildReaperRushDefense()
        {
            BuildList result = new BuildList();

            result.If(() => { return DefendReapers; });
            result.Building(UnitTypes.PYLON, Main);
            result.Building(UnitTypes.PYLON, Natural, 3);
            result.Building(UnitTypes.FORGE);
            result.Building(UnitTypes.PHOTON_CANNON, Natural, 5);
            result.Building(UnitTypes.PYLON, Main);
            result.Building(UnitTypes.PHOTON_CANNON, Main);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.PHOTON_CANNON, Natural);
            result.Building(UnitTypes.PYLON, Main);
            result.Building(UnitTypes.PHOTON_CANNON, Main);

            return result;
        }

        private BuildList PowerPylons()
        {
            BuildList result = new BuildList();

            result.If(() => { return Minerals() >= 300 
                && Count(UnitTypes.STARGATE) < 3
                && Count(UnitTypes.CYBERNETICS_CORE) > 0
                && Count(UnitTypes.PHOTON_CANNON) >= 4
                && Count(UnitTypes.PYLON) < 5
                && Count(UnitTypes.PYLON) == Completed(UnitTypes.PYLON); });
            result.Building(UnitTypes.PYLON);
            result.Goto(0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Main);
            result.Building(UnitTypes.GATEWAY);
            if (SkipDefenses || Tyr.Bot.EnemyRace == Race.Zerg)
                result.Building(UnitTypes.NEXUS, 2);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.FLEET_BEACON, () => { return BuildCarriers; });
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.STARGATE, () => { return Count(UnitTypes.CARRIER) + Count(UnitTypes.VOID_RAY) >= 2; });

            return result;
        }

        private BuildList HiddenBasePylons()
        {
            BuildList result = new BuildList();

            result.If(() =>
            { return FarBase.ResourceCenter != null && FarBase.ResourceCenter.Unit.BuildProgress >= 0.99; });
            result.If(() =>
            {
                return FoodUsed()
                    + Tyr.Bot.UnitManager.Count(UnitTypes.NEXUS)
                    + Tyr.Bot.UnitManager.Count(UnitTypes.GATEWAY) * 2
                    + Tyr.Bot.UnitManager.Count(UnitTypes.STARGATE) * 2
                    + Tyr.Bot.UnitManager.Count(UnitTypes.ROBOTICS_FACILITY) * 2
                    >= ExpectedAvailableFood() - 2;
            });
            result += new BuildingStep(UnitTypes.PYLON, FarBase);
            result.Goto(0);

            return result;
        }

        private BuildList HiddenBaseBuild()
        {
            BuildList result = new BuildList();

            result.If(() =>
            { return FarBase.ResourceCenter != null && FarBase.ResourceCenter.Unit.BuildProgress >= 0.99; });
            result.Building(UnitTypes.PYLON, FarBase);
            result.Building(UnitTypes.GATEWAY, FarBase);
            result.Building(UnitTypes.FORGE, FarBase);
            result.Building(UnitTypes.CYBERNETICS_CORE, FarBase);
            result.Building(UnitTypes.PYLON, FarBase);
            result.Building(UnitTypes.PHOTON_CANNON, FarBase, 4);
            result.Building(UnitTypes.ASSIMILATOR, FarBase, 2);
            result.Building(UnitTypes.PYLON, FarBase);
            result.Building(UnitTypes.STARGATE, FarBase);
            result.Building(UnitTypes.FLEET_BEACON, FarBase, () => { return BuildCarriers; });
            result.Building(UnitTypes.PHOTON_CANNON, FarBase, 2);
            result.Building(UnitTypes.STARGATE, FarBase);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(948);
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(950);
            tyr.buildingPlacer.BuildCompact = true;

            FlyerAttackTask.Task.RequiredSize = RequiredSize;
            IdleTask.Task.FearEnemies = true;

            DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.VIKING_FIGHTER);

            FlyerDestroyTask.Task.Stopped = !DefendReapers;
            FlyerAttackTask.Task.Stopped = DefendReapers;

            if (Count(UnitTypes.PROBE) <= 18)
                BaseWorkers.WorkersPerGas = 0;
            else
                BaseWorkers.WorkersPerGas = 3;

            if (tyr.EnemyRace == Race.Zerg)
                Beyond2Cannons = tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) > 0 || Count(UnitTypes.STARGATE) >= 3;

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
                

            if (!DefendRush && tyr.Frame <= 4800 && Tyr.Bot.EnemyRace != Race.Zerg)
            {
                int enemyCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                    if (SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 40 * 40)
                        enemyCount++;

                if (enemyCount >= 3)
                    DefendRush = true;
            }
            
            if ((tyr.EnemyRace == Race.Terran || tyr.EnemyRace == Race.Random)
                && ReaperDefenseCannonStep.DesiredPos == null
                && !SkipDefenses)
            {
                foreach (Unit unit in tyr.Enemies())
                {
                    if (unit.UnitType == UnitTypes.REAPER
                        && Tyr.Bot.MapAnalyzer.StartArea[(int)System.Math.Round(unit.Pos.X), (int)System.Math.Round(unit.Pos.Y)])
                    {
                        Point2D dir = SC2Util.Point(unit.Pos.X - tyr.MapAnalyzer.StartLocation.X, unit.Pos.Y - tyr.MapAnalyzer.StartLocation.Y);
                        float length = (float)System.Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                        dir = SC2Util.Point(dir.X / length, dir.Y / length);

                        ReaperDefenseCannonStep.DesiredPos = SC2Util.Point(tyr.MapAnalyzer.StartLocation.X + dir.X * 4f, tyr.MapAnalyzer.StartLocation.Y + dir.Y * 4f);
                        break;
                    }
                }
            }

            if (!DefendReapers && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.REAPER) >= 2)
            {
                FearVikingsController.Stopped = false;
                DefendReapers = true;
                tyr.buildingPlacer.SpreadCannons = false;
                tyr.buildingPlacer.BuildCompact = true;
                WorkerTask.Task.StopTransfers = true;
                HideBaseTask.Task.BuildNexus = true;

                IdleTask.Task.OverrideTarget = FarBase.BaseLocation.Pos;
                DefenseTask.AirDefenseTask.Stopped = true;
                RequiredSize = 8;

                Set = new BuildSet();
                Set += HiddenBasePylons();
                Set += HiddenBaseBuild();
            }
            /*
            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.REAPER) >= 2
                && NaturalMirrorCannonStep.DesiredBase == null)
            {
                PotentialHelper helper = new PotentialHelper(tyr.BaseManager.Natural.BaseLocation.Pos);
                helper.Magnitude = 8;
                helper.From(tyr.BaseManager.NaturalDefensePos);
                NaturalMirrorCannonStep.DesiredPos = helper.Get();
                NaturalMirrorPylonStep.DesiredPos = helper.Get();
            }
            */
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < (SkipDefenses ? 31 : 39)
                && (Count(UnitTypes.PROBE) < 32 || Count(UnitTypes.CARRIER) + Count(UnitTypes.VOID_RAY) > 0)
                && (Count(UnitTypes.PROBE) < 20 || Count(UnitTypes.CYBERNETICS_CORE) > 0)
                && (Count(UnitTypes.PROBE) < 16 || Count(UnitTypes.NEXUS) >= 2)
                && (!DefendReapers || agent.Unit.AssignedHarvesters < 16)
                && (!DefendReapers || agent == FarBase.ResourceCenter))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            if (agent.Unit.UnitType == UnitTypes.STARGATE)
            {
                if (DefendReapers && agent.Base != FarBase)
                    return;
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
                    && Count(UnitTypes.CARRIER) >= 5)
                    agent.Order(44);
            }
            if (agent.Unit.UnitType == UnitTypes.CYBERNETICS_CORE)
            {
                if (Count(UnitTypes.VOID_RAY) + Count(UnitTypes.CARRIER) >= 4)
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
