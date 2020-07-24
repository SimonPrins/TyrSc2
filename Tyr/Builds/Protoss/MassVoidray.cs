using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
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
        public bool BuildCarriers = false;
        private bool Beyond2Cannons = true;
        public bool NaturalWall = false;
        public bool CloseWall = false;
        public Test BuildDefenses;
        private WallInCreator WallIn;
        public bool Recalled = false;
        public bool DelayNatural = false;

        public override string Name()
        {
            return "MassVoidray";
        }
        public delegate bool Test();

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            FlyerAttackTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            WorkerSafetyTask.Enable();
            WorkerScoutTask.Enable();
            RecallTask.Enable();

            FlyerDestroyTask.Enable();
            if (Bot.Main.EnemyRace == Race.Protoss)
                ProxySpotterTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            if (BuildDefenses == null)
                BuildDefenses = () => !SkipDefenses;
            tyr.Monday = true;

            MicroControllers.Add(FearVikingsController);
            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new CarrierController());
            MicroControllers.Add(new StutterController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.CreateFullNatural(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                WallIn.ReserveSpace();
                if (NaturalWall)
                    tyr.buildingPlacer.BuildInsideWall(WallIn);
            }

            Set += NaturalDefenses();
            Set += RushDefenses();
            Set += ProtossBuildUtil.Pylons();
            Set += BuildReaperDefenseCannon();
            Set += BuildReaperRushDefense();
            Set += ProtossBuildUtil.Nexus(2, () => Count(UnitTypes.GATEWAY) > 0 && (!DelayNatural || Count(UnitTypes.STARGATE) > 0));
            Set += PowerPylons();
            Set += MainBuild();
        }

        private BuildList NaturalDefenses()
        {
            BuildList result = new BuildList();

            result.If(() => BuildDefenses());
            if (NaturalWall)
                result.If(() => Completed(UnitTypes.FORGE) > 0);
            if (!NaturalWall)
            {
                result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
                result.Building(UnitTypes.FORGE, Natural, NaturalDefensePos);
            }
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2);
            if (NaturalWall)
                result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.If(() => { return Count(UnitTypes.NEXUS) >= 2 && Beyond2Cannons; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, () => { return !DefendReapers; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, () => { return !DefendReapers; });
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos);
            result.If(() => { return !DefendReapers; });
            result.If(() => { return Minerals() >= 650 && Bot.Main.Frame % 9 == 0; });
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

            result.If(() => BuildDefenses());
            result.If(() => { return Count(UnitTypes.FORGE) > 0 && DefendRush; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 4);
            result.If(() => { return !DefendReapers; });
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2);

            return result;
        }

        private BuildList BuildReaperDefenseCannon()
        {
            BuildList result = new BuildList();

            result.If(() => BuildDefenses());
            Base reaperBase = null;
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b != Bot.Main.BaseManager.Main && b != Bot.Main.BaseManager.Natural)
                    reaperBase = b;

            Base naturalMirrorBase = null;
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b != Bot.Main.BaseManager.Main && b != Bot.Main.BaseManager.Natural
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

            result.If(() => BuildDefenses());
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

            if (NaturalWall)
            {
                result.Building(UnitTypes.PYLON, Natural, WallIn.Wall[2].Pos, true);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[0].Pos, true);
                if (!BuildDefenses())
                    result.Building(UnitTypes.NEXUS, 2, () => !DelayNatural || Count(UnitTypes.STARGATE) > 0);

                result.Building(UnitTypes.ASSIMILATOR);
                result.Building(UnitTypes.CYBERNETICS_CORE, Natural, WallIn.Wall[1].Pos, true);
                result.Building(UnitTypes.ASSIMILATOR);
                result.Building(UnitTypes.FORGE, Natural, WallIn.Wall[3].Pos, true);
            }
            else
            {
                result.Building(UnitTypes.PYLON, Main);
                result.Building(UnitTypes.GATEWAY);
                if (!BuildDefenses())
                    result.Building(UnitTypes.NEXUS, 2, () => !DelayNatural || Count(UnitTypes.STARGATE) > 0);
                result.Building(UnitTypes.ASSIMILATOR);
                result.Building(UnitTypes.CYBERNETICS_CORE);
                result.Building(UnitTypes.ASSIMILATOR);
            }
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.FLEET_BEACON, () => { return BuildCarriers; });
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.STARGATE, () => { return Count(UnitTypes.CARRIER) + Count(UnitTypes.VOID_RAY) >= 2; });

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(948);
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(950);
            tyr.buildingPlacer.BuildCompact = true;

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            FlyerAttackTask.Task.RequiredSize = RequiredSize;
            IdleTask.Task.FearEnemies = true;

            DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.VIKING_FIGHTER);
            if (TotalEnemyCount(UnitTypes.TEMPEST) > 0)
            {
                DefenseTask.AirDefenseTask.MainDefenseRadius = 50;
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 50;
                DefenseTask.AirDefenseTask.BufferZone = 20;
            }
            FlyerDestroyTask.Task.Stopped = !DefendReapers;
            FlyerAttackTask.Task.Stopped = DefendReapers;

            if (Count(UnitTypes.PROBE) <= 14)
                GasWorkerTask.WorkersPerGas = 0;
            else
                GasWorkerTask.WorkersPerGas = 3;

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
                

            if (!DefendRush && tyr.Frame <= 4800 && Bot.Main.EnemyRace != Race.Zerg)
            {
                int enemyCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                    if (SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 40 * 40)
                        enemyCount++;

                if (enemyCount >= 3)
                    DefendRush = true;
            }
            
            if ((tyr.EnemyRace == Race.Terran)
                && ReaperDefenseCannonStep != null
                && ReaperDefenseCannonStep.DesiredPos == null
                && BuildDefenses())
            {
                foreach (Unit unit in tyr.Enemies())
                {
                    if (unit.UnitType == UnitTypes.REAPER
                        && Bot.Main.MapAnalyzer.StartArea[(int)System.Math.Round(unit.Pos.X), (int)System.Math.Round(unit.Pos.Y)])
                    {
                        Point2D dir = SC2Util.Point(unit.Pos.X - tyr.MapAnalyzer.StartLocation.X, unit.Pos.Y - tyr.MapAnalyzer.StartLocation.Y);
                        float length = (float)System.Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                        dir = SC2Util.Point(dir.X / length, dir.Y / length);

                        ReaperDefenseCannonStep.DesiredPos = SC2Util.Point(tyr.MapAnalyzer.StartLocation.X + dir.X * 4f, tyr.MapAnalyzer.StartLocation.Y + dir.Y * 4f);
                        break;
                    }
                }
            }

            if (!Recalled
                && Completed(UnitTypes.PHOTON_CANNON) <= 2
                && DefenseTask.AirDefenseTask.Units.Count + DefenseTask.GroundDefenseTask.Units.Count <= 4
                && TimingAttackTask.Task.Units.Count >= 6)
            {
                int enemyAttackingUnits = 0;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) >= 60 * 60)
                        continue;
                    enemyAttackingUnits++;
                }

                if (enemyAttackingUnits >= 5)
                {
                    Point2D averagePos = new Point2D();
                    foreach (Agent agent in TimingAttackTask.Task.Units)
                    {
                        averagePos.X += agent.Unit.Pos.X;
                        averagePos.Y += agent.Unit.Pos.Y;
                    }
                    averagePos.X /= TimingAttackTask.Task.Units.Count;
                    averagePos.Y /= TimingAttackTask.Task.Units.Count;
                    Point2D recallPos = null;
                    float dist = 1000000;
                    foreach (Agent agent in TimingAttackTask.Task.Units)
                    {
                        float newDist = agent.DistanceSq(averagePos);
                        if (newDist >= dist)
                            continue;
                        dist = newDist;
                        recallPos = SC2Util.To2D(agent.Unit.Pos);
                    }
                    RecallTask.Task.Location = recallPos;
                    Recalled = true;
                    System.Console.WriteLine("Recalling.");
                }
            }
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < (BuildDefenses() ? 39 : 31)
                && (Count(UnitTypes.PROBE) < 32 || Count(UnitTypes.CARRIER) + Count(UnitTypes.VOID_RAY) > 0)
                && (Count(UnitTypes.PROBE) < 20 || Count(UnitTypes.CYBERNETICS_CORE) > 0)
                && (Count(UnitTypes.PROBE) < 16 || Count(UnitTypes.NEXUS) >= 2)
                && (!DefendReapers || agent.Unit.AssignedHarvesters < 16))
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
