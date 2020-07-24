using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Terran
{
    public class TankPushTvP : Build
    {
        private WallInCreator WallIn;

        private bool ZealotRushSuspected = false;
        private bool ZealotRushConfirmed = false;
        private bool WorkerRushDetected = false;

        private bool CannonsDetected = false;
        private bool GatewayPushDetected = false;

        private int OraclesDetectedFrame = -9999;
        private bool OraclesDetected = false;

        private int DesiredVikings = 3;

        private bool ScanTimingsSet = false;
        private bool EarlyScanSet = false;

        private bool DTsSuspected = false;
        private bool SkyTossDetected = false;

        private bool CollosusSuspected = false;

        private bool FourGateStalkerSuspected = false;
        private bool StalkerEmergency = false;

        private bool EcoCheeseSuspected = false;

        private MarineHarassController MarineHarassController = new MarineHarassController();

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            WorkerScoutTask.Enable();
            DefenseTask.Enable();
            BunkerDefendersTask.Enable();
            SupplyDepotTask.Enable();
            ArmyRavenTask.Enable();
            //MechDestroyExpandsTask.Enable();
            RepairTask.Enable();
            ReplenishBuildingSCVTask.Enable();
            TransformTask.Enable();
            WorkerRushDefenseTask.Enable();
            TimingAttackTask.Enable();
            DefenseSquadTask.Enable(false, UnitTypes.MARINE);
            SiegeAtRampTask.Enable();
            SiegeBelowRampTask.Enable();
            HomeRepairTask.Enable();
        }

        public override string Name()
        {
            return "TankPushTvP";
        }

        public override void OnStart(Bot tyr)
        {
            TimingAttackTask.Task.BeforeControllers.Add(new LeashController(new HashSet<uint>() { UnitTypes.LIBERATOR, UnitTypes.MEDIVAC, UnitTypes.CYCLONE, UnitTypes.VIKING_FIGHTER },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED },
                4));
            TimingAttackTask.Task.BeforeControllers.Add(new LeashController(new HashSet<uint>() { UnitTypes.MARAUDER, UnitTypes.MARINE },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED },
                8));
            TimingAttackTask.Task.BeforeControllers.Add(new SoftLeashController(new HashSet<uint>() { UnitTypes.LIBERATOR, UnitTypes.MEDIVAC, UnitTypes.CYCLONE, UnitTypes.MARAUDER, UnitTypes.MARINE, UnitTypes.VIKING_FIGHTER },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED },
                12));

            MicroControllers.Add(new FearEnemyController(new HashSet<uint>() { UnitTypes.MARINE, UnitTypes.CYCLONE }, UnitTypes.PHOTON_CANNON, 10));
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new LiberatorController());
            MicroControllers.Add(new VikingController() { StickToTanks = false });
            MicroControllers.Add(new MedivacController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(MarineHarassController);

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT, UnitTypes.BUNKER });
                WallIn.ReserveSpace();
            }

            Set += SupplyDepots();
            Set += Turrets();
            Set += AntiWorkerRush();
            Set += AntiTempestBuild();
            Set += AntiCannonBuild();
            Set += MainBuild();
        }

        public BuildList SupplyDepots()
        {
            BuildList result = new BuildList();

            result.If(() => { return Count(UnitTypes.SUPPLY_DEPOT) >= 2 || WorkerRushDetected; });
            result.If(() =>
            {
                return Build.FoodUsed()
                    + Bot.Bot.UnitManager.Count(UnitTypes.COMMAND_CENTER)
                    + Bot.Bot.UnitManager.Count(UnitTypes.BARRACKS) * 2
                    + Bot.Bot.UnitManager.Count(UnitTypes.FACTORY) * 2
                    + Bot.Bot.UnitManager.Count(UnitTypes.STARPORT) * 2
                    >= Build.ExpectedAvailableFood() - 2
                    && Build.ExpectedAvailableFood() < 200;
            });
            result += new BuildingStep(UnitTypes.SUPPLY_DEPOT);
            result.Goto(0);

            return result;
        }

        private BuildList Turrets()
        {
            BuildList result = new BuildList();

            result.If(() => { return DTsSuspected && Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.CYCLONE) > 0 && !StalkerEmergency; });
            result.Building(UnitTypes.ENGINEERING_BAY);
            foreach (Base b in Bot.Bot.BaseManager.Bases)
                if (b != Main)
                    result.Building(UnitTypes.MISSILE_TURRET, b, () => { return b.Owner == Bot.Bot.PlayerId && b.ResourceCenter != null; });
            return result;

        }

        private BuildList AntiWorkerRush()
        {
            BuildList result = new BuildList();

            result.If(() => StrategyAnalysis.WorkerRush.Get().Detected);
            result.Building(UnitTypes.SUPPLY_DEPOT);
            result.Building(UnitTypes.BARRACKS);

            return result;
        }

        private BuildList AntiTempestBuild()
        {
            BuildList result = new BuildList();

            result.If(() => Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) > 0 || SkyTossDetected);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.BUNKER, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.ARMORY);
            result.If(() => Minerals() >= 350 || Count(UnitTypes.THOR) >= 2);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.STARPORT, () => { return !ZealotRushSuspected || CannonsDetected; });

            return result;
        }

        private BuildList AntiCannonBuild()
        {
            BuildList result = new BuildList();

            result.If(() => { return CannonsDetected && !SkyTossDetected; });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.BUNKER, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.If(() => Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.CYCLONE) >= Completed(UnitTypes.SIEGE_TANK) + Completed(UnitTypes.CYCLONE) +1);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.STARPORT, () => DTsSuspected || SkyTossDetected);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.BARRACKS, 2);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);

            return result;

        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.If(() => { return !CannonsDetected && Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) == 0 && (!StrategyAnalysis.WorkerRush.Get().Detected || Count(UnitTypes.MARINE) >= 5) && !SkyTossDetected; });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            //result.Building(UnitTypes.BUNKER, Natural, NaturalDefensePos, true);
            result.Building(UnitTypes.BUNKER, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.FACTORY, () => !GatewayPushDetected || Count(UnitTypes.MARAUDER) >= 2);
            result.Building(UnitTypes.REFINERY, () => StalkerEmergency);
            result.Building(UnitTypes.ARMORY, () => FourGateStalkerSuspected && Completed(UnitTypes.SIEGE_TANK) > 2);
            result.If(() => !StalkerEmergency || Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.STARGATE) > 0);
            result.If(() => !ZealotRushConfirmed || Completed(UnitTypes.HELLBAT) + Completed(UnitTypes.HELLION) + Completed(UnitTypes.MARAUDER) >= 6);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, () => !StalkerEmergency || Completed(UnitTypes.SIEGE_TANK) >= 2);
            result.Building(UnitTypes.REFINERY, 2, () => FourGateStalkerSuspected && Completed(UnitTypes.SIEGE_TANK) >= 2);
            result.Building(UnitTypes.STARPORT, () => { return OraclesDetected || CollosusSuspected || SkyTossDetected; });
            result.If(() => { return !OraclesDetected || Completed(UnitTypes.VIKING_FIGHTER) > 0; });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.If(() => Completed(UnitTypes.COMMAND_CENTER) >= 3);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.STARPORT, () => { return !SkyTossDetected && !CollosusSuspected && !OraclesDetected && (!ZealotRushSuspected || CannonsDetected); });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.BARRACKS, 2);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.STARPORT, () => { return !ZealotRushSuspected || CannonsDetected; });
            result.Building(UnitTypes.BARRACKS);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (tyr.Observation.ActionErrors != null)
                foreach (ActionError error in tyr.Observation.ActionErrors)
                    DebugUtil.WriteLine("Error with ability " + error.AbilityId + ": " + error.Result);

            if (!EcoCheeseSuspected && Expanded.Get().Detected && tyr.Frame <= 22.4 * 60 * 2 && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.GATEWAY) == 0)
                EcoCheeseSuspected = true;
            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.STALKER) + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ADEPT) > 0 
                || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ZEALOT) >= 3)
                EcoCheeseSuspected = false;

            BunkerDefendersTask.Task.LeaveBunkers = EcoCheeseSuspected;
            MarineHarassController.Disabled = !EcoCheeseSuspected;

            if ((tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.STALKER) >= 7 || (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.GATEWAY) >= 3 && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.CYBERNETICS_CORE) > 0))
                && tyr.Frame < 22.4 * 60f * 3.5)
                FourGateStalkerSuspected = true;

            StalkerEmergency = FourGateStalkerSuspected
                && (Completed(UnitTypes.SIEGE_TANK) < 2 || Completed(UnitTypes.MARAUDER) < 4)
                && Completed(UnitTypes.MARAUDER) + Completed(UnitTypes.MARINE) + Completed(UnitTypes.SIEGE_TANK) + Completed(UnitTypes.CYCLONE) < 15;

            if (FourGateStalkerSuspected
                && ((Completed(UnitTypes.SIEGE_TANK) >= 2 && Completed(UnitTypes.MARAUDER) >= 3) || Count(UnitTypes.COMMAND_CENTER) >= 2))
                IdleTask.Task.OverrideTarget = SC2Util.Point((tyr.MapAnalyzer.GetMainRamp().X + Natural.BaseLocation.Pos.X) / 2f, (tyr.MapAnalyzer.GetMainRamp().Y + Natural.BaseLocation.Pos.Y) / 2f);
            else IdleTask.Task.OverrideTarget = null;

            SiegeAtRampTask.Task.Stopped = !FourGateStalkerSuspected || Count(UnitTypes.THOR) > 0;
            SiegeBelowRampTask.Task.Stopped = !FourGateStalkerSuspected || Count(UnitTypes.THOR) > 0;
            if (FourGateStalkerSuspected
                && Completed(UnitTypes.SIEGE_TANK) < 2
                && Completed(UnitTypes.THOR) < 2
                && Count(UnitTypes.COMMAND_CENTER) > Completed(UnitTypes.COMMAND_CENTER)
                && Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.STARGATE) == 0)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }

            tyr.TargetManager.TargetCannons = true;
            if (!OraclesDetected
                && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ORACLE) > 0)
                OraclesDetectedFrame = tyr.Frame;
            OraclesDetected = tyr.Frame - OraclesDetectedFrame < 30 * 22.4;

            if (Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.COLOSUS)
                + Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ROBOTICS_BAY)
                + Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ROBOTICS_FACILITY) > 0)
                CollosusSuspected = true;
            if (CollosusSuspected
                || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.TEMPEST) > 0
                || CannonsDetected)
                DesiredVikings = 10;
            else if (SkyTossDetected)
                DesiredVikings = 5;

            foreach (Task task in DefenseSquadTask.Tasks)
            {
                task.Stopped = !OraclesDetected;
                if (!OraclesDetected)
                    task.Clear();
                task.Priority = 8;
            }

            if (ZealotRushConfirmed)
            {
                TimingAttackTask.Task.RequiredSize = 20;
                TimingAttackTask.Task.RetreatSize = 6;
            }
            else if (EcoCheeseSuspected)
            {
                TimingAttackTask.Task.RequiredSize = 3;
                TimingAttackTask.Task.RetreatSize = 0;
            }
            else if(Completed(UnitTypes.COMMAND_CENTER) < 2
                || FourGateStalkerSuspected
                || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) > 0)
            {
                TimingAttackTask.Task.RequiredSize = 40;
                TimingAttackTask.Task.RetreatSize = 10;
            }
            else if (CollosusSuspected)
            {
                if (Completed(UnitTypes.VIKING_FIGHTER) == 0)
                {
                    TimingAttackTask.Task.Clear();
                    TimingAttackTask.Task.Stopped = true;
                } else
                    TimingAttackTask.Task.Stopped = false;
                TimingAttackTask.Task.RequiredSize = 40;

            }
            else if (WorkerRushDetected)
            {
                TimingAttackTask.Task.RequiredSize = 8;
                TimingAttackTask.Task.RetreatSize = 0;
            }
            else if (DTsSuspected)
            {
                TimingAttackTask.Task.RequiredSize = 25;
                TimingAttackTask.Task.RetreatSize = 8;
            }
            else if (CannonsDetected)
            {
                TimingAttackTask.Task.RequiredSize = 15;
                TimingAttackTask.Task.RetreatSize = 8;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 30;
                TimingAttackTask.Task.RetreatSize = 10;
            }

            tyr.DrawText("Attack sent: " + TimingAttackTask.Task.AttackSent);
            tyr.DrawText("RequiredSize: " + TimingAttackTask.Task.RequiredSize);

            if (!GatewayPushDetected)
            {
                if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.CYBERNETICS_CORE) > 0
                    && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.GATEWAY) >= 3)
                    GatewayPushDetected = true;

                if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ADEPT) > 0)
                    GatewayPushDetected = true;
                if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.STALKER) >= 3)
                    GatewayPushDetected = true;
            }

            if (GatewayPushDetected)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.GATEWAY) > 0
                && tyr.Frame >= 22.4 * 90
                && tyr.Frame < 22.4 * 110)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ASSIMILATOR) + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.CYBERNETICS_CORE) > 0
                && tyr.Frame >= 22.4 * 75
                && tyr.Frame < 22.4 * 110)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if ((ZealotRushSuspected
                || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) > 0
                || WorkerRushDetected)
                && tyr.Frame >= 22.4 * 110)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (FourGateStalkerSuspected)
            {
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 15;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 15;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            }
            else
            {
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            }
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 80;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 80;

            RepairTask.Task.WallIn = WallIn;

            TransformTask.Task.HellionsToHellbats();
            TransformTask.Task.ThorsToSingleTarget();

            if (!ZealotRushConfirmed && Bot.Bot.EnemyRace == Race.Protoss && tyr.Frame < 22.4 * 60 * 4 && Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZEALOT) >= 5)
                ZealotRushConfirmed = true;

            if (ZealotRushConfirmed && Completed(UnitTypes.HELLION) + Completed(UnitTypes.HELLBAT) + Completed(UnitTypes.MARAUDER) < 6)
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER && agent.Unit.BuildProgress < 0.99 && agent.DistanceSq(Main.BaseLocation.Pos) >= 4 * 4)
                        agent.Order(Abilities.CANCEL);

            if (!ZealotRushSuspected && Bot.Bot.EnemyRace == Race.Protoss)
            {
                if ((Bot.Bot.Frame >= 22.4 * 60 * 1.5
                    && !Bot.Bot.EnemyStrategyAnalyzer.NoProxyGatewayConfirmed)
                    || (Bot.Bot.Frame < 22.4 * 60 * 1.5 && ThreeGate.Get().Detected))
                    ZealotRushSuspected = true;
            }

            if (!CannonsDetected)
            {
                if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) > 0
                    || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.FORGE) > 0)
                    CannonsDetected = true;
            }

            if (WorkerRushDefenseTask.Task.WorkerRushHappening)
                WorkerRushDetected = true;

            tyr.OrbitalAbilityManager.SaveEnergy = 100;
            if (tyr.Frame >= (int)(22.4 * 60f * 2.5)
                && !EarlyScanSet
                && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.GATEWAY) == 1
                && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.FORGE) == 0
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.STALKER) == 0
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ADEPT) == 0
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ADEPT_PHASE_SHIFT) == 0)
            {
                EarlyScanSet = true;
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 3)
                });
            }

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.GATEWAY) >= 2 
                && (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.CYBERNETICS_CORE) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ASSIMILATOR) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.STALKER) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ADEPT) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ADEPT_PHASE_SHIFT) > 0)
                && tyr.Frame >= 22.4 * 60f * 2.5
                && !ScanTimingsSet)
            {
                ScanTimingsSet = true;
                if (!EarlyScanSet)
                {
                    EarlyScanSet = true;
                    tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                    {
                        Pos = tyr.TargetManager.PotentialEnemyStartLocations[0],
                        FromFrame = (int)(22.4 * 60 * 3)
                    });
                }
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 11.5)
                });
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.MapAnalyzer.GetEnemyNatural().Pos,
                    FromFrame = (int)(22.4 * 60 * 11.5 + 22.4)
                });

            }

            if (!GatewayPushDetected && !ScanTimingsSet && tyr.Frame >= 22.4 * 60f * 2.75)
            {
                ScanTimingsSet = true;
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 4)
                });
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 5.5)
                });
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.MapAnalyzer.GetEnemyNatural().Pos,
                    FromFrame = (int)(22.4 * 60 * 5.5 + 22.4)
                });
            }

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.DARK_SHRINE)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.DARK_TEMPLAR)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.TWILIGHT_COUNSEL) > 0)
                DTsSuspected = true;

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.STARGATE)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.VOID_RAY)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.CARRIER)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.INTERCEPTOR)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.TEMPEST)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOENIX)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ORACLE) > 0)
                SkyTossDetected = true;

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) > 0)
            {
                TimingAttackTask.Task.BeforeControllers = new List<CustomController>();
                TransformTask.Task.Priority = 10;
            }
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Completed(UnitTypes.BARRACKS) > 0
                && Count(UnitTypes.SCV) >= 16
                && (agent.Base == Main
                    || (agent.Base == Natural && !FourGateStalkerSuspected)))
            {
                if (Minerals() >= 150)
                    agent.Order(1516);
            }
            else if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Completed(UnitTypes.ENGINEERING_BAY) > 0
                && Count(UnitTypes.SCV) >= 16
                && Gas() >= 150
                && agent.Base != Main
                && (agent.Base != Natural || FourGateStalkerSuspected))
            {
                if (Minerals() >= 150)
                    agent.Order(1450);
            }
            else if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Count(UnitTypes.SCV) < Math.Min(60, 20 * Count(UnitTypes.COMMAND_CENTER))
                    && Minerals() >= 50
                    && FoodLeft() >= 1)
                    agent.Order(524);
            }
            else if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (SkyTossDetected
                    && Count(UnitTypes.VIKING_FIGHTER) == 0
                    && Completed(UnitTypes.STARPORT) == 0
                    && Minerals() <= 200)
                    return;
                if (SkyTossDetected
                    && Completed(UnitTypes.STARPORT_REACTOR) != 0
                    && Count(UnitTypes.VIKING_FIGHTER) + Count(UnitTypes.LIBERATOR) + Count(UnitTypes.MEDIVAC) < Completed(UnitTypes.VIKING_FIGHTER) + Completed(UnitTypes.LIBERATOR) + Completed(UnitTypes.MEDIVAC) + 2
                    && Minerals() <= 200)
                    return;
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.MARINE) >= 4 && !WorkerRushDetected)
                    {
                        if (Count(UnitTypes.BARRACKS_REACTOR) < Count(UnitTypes.BARRACKS_TECH_LAB)
                            || SkyTossDetected)
                            agent.Order(422);
                        else
                            agent.Order(421);
                    }
                    else if (Minerals() >= 50
                        && FoodLeft() >= 1
                        && (!CannonsDetected || Count(UnitTypes.SIEGE_TANK) > 0))
                        agent.Order(560);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.BARRACKS_TECH_LAB)
                {
                    if (Minerals() >= 100
                        && Gas() >= 25
                        && FoodLeft() >= 2
                        && (!CannonsDetected || Count(UnitTypes.SIEGE_TANK) > 0)
                        && (!OraclesDetected || Count(UnitTypes.VIKING_FIGHTER) > 0)
                        && !SkyTossDetected)
                        agent.Order(563);
                    else if (SkyTossDetected
                        && Minerals() >= 50
                        && FoodLeft() >= 1
                        && (!CannonsDetected || Count(UnitTypes.SIEGE_TANK) > 0)
                        && (!OraclesDetected || Count(UnitTypes.VIKING_FIGHTER) > 0 || Count(UnitTypes.MARINE) < 8))
                        agent.Order(560);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.BARRACKS_REACTOR)
                {
                    if (StalkerEmergency && Completed(UnitTypes.SIEGE_TANK) >= 2 && Completed(UnitTypes.MARAUDER) >= 4
                        && Count(UnitTypes.COMMAND_CENTER) < 2)
                        return;
                    if (Minerals() >= 50
                        && FoodLeft() >= 1
                        && (!OraclesDetected || Count(UnitTypes.VIKING_FIGHTER) > 0 || Count(UnitTypes.MARINE) < 8)
                        && (Completed(UnitTypes.FACTORY_TECH_LAB) == 0 || Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.CYCLONE) >= Completed(UnitTypes.SIEGE_TANK) + Completed(UnitTypes.CYCLONE) + 1)
                        && (Completed(UnitTypes.STARPORT_REACTOR) == 0 || Count(UnitTypes.VIKING_FIGHTER) + Count(UnitTypes.LIBERATOR) + Count(UnitTypes.MEDIVAC) >= Completed(UnitTypes.VIKING_FIGHTER) + Completed(UnitTypes.LIBERATOR) + Completed(UnitTypes.MEDIVAC) + 2))
                        agent.Order(560);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY)
            {
                if (SkyTossDetected
                    && tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag)
                    && tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB
                    && Count(UnitTypes.CYCLONE) < 2)
                {
                    if (Minerals() >= 150
                        && Gas() >= 100
                        && FoodLeft() >= 3)
                        agent.Order(597);
                    return;
                }
                if (SkyTossDetected
                    && Count(UnitTypes.VIKING_FIGHTER) == 0
                    && (Completed(UnitTypes.STARPORT_REACTOR) == 0 || Count(UnitTypes.VIKING_FIGHTER) >= Completed(UnitTypes.VIKING_FIGHTER) + 2)
                    && Minerals() <= 300)
                    return;
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (CannonsDetected)
                        agent.Order(454);
                    else if (Count(UnitTypes.FACTORY_TECH_LAB) <= Count(UnitTypes.FACTORY_REACTOR) || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) > 0)
                    {
                        if (!ZealotRushSuspected || CannonsDetected)
                            agent.Order(454);
                        else
                        {
                            if (Completed(UnitTypes.ARMORY) > 0
                                && Minerals() >= 100
                                && FoodLeft() >= 2)
                                agent.Order(596);
                            else if (Minerals() >= 100
                                && FoodLeft() >= 2)
                                agent.Order(595);
                        }
                    }
                    else
                        agent.Order(455);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                {
                    if ((!CannonsDetected || Count(UnitTypes.SIEGE_TANK) <= Count(UnitTypes.CYCLONE))
                        && (CannonsDetected || Count(UnitTypes.SIEGE_TANK) < 3 || !SkyTossDetected)
                        && (Count(UnitTypes.SIEGE_TANK) < 2 || !SkyTossDetected || Count(UnitTypes.THOR) >= 2)
                        && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) == 0
                        && (!FourGateStalkerSuspected || Count(UnitTypes.SIEGE_TANK) <= Count(UnitTypes.CYCLONE) || Count(UnitTypes.SIEGE_TANK) < 3))
                    {
                        if (Minerals() >= 150
                            && Gas() >= 125
                            && FoodLeft() >= 3
                            && (!OraclesDetected || Count(UnitTypes.VIKING_FIGHTER) > 0))
                                agent.Order(591);
                    }
                    else if ((FourGateStalkerSuspected || SkyTossDetected)
                        && (!SkyTossDetected || Count(UnitTypes.CYCLONE) >= 2)
                        && Completed(UnitTypes.ARMORY) > 0)
                    {
                        if (Minerals() >= 300
                            && Gas() >= 200
                            && FoodLeft() >= 6)
                            agent.Order(594);
                    } else
                    {
                        if (Minerals() >= 150
                            && Gas() >= 100
                            && FoodLeft() >= 3)
                            agent.Order(597);
                    }
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR
                    && Minerals() >= 100
                    && (!SkyTossDetected || Count(UnitTypes.THOR) >= 2))
                {
                    if (Completed(UnitTypes.ARMORY) > 0
                        && Minerals() >= 100
                        && FoodLeft() >= 2)
                        agent.Order(596);
                    else if (Minerals() >= 100
                        && FoodLeft() >= 2)
                        agent.Order(595);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (DTsSuspected && Count(UnitTypes.STARPORT_TECH_LAB) == 0)
                    {
                        if (Minerals() >= 50
                            && Gas() >= 50)
                            agent.Order(487);
                    }
                    else if ((SkyTossDetected || !CannonsDetected) && Count(UnitTypes.VIKING_FIGHTER) > 0)
                    {
                        if (Count(UnitTypes.STARPORT_REACTOR) == 0 || Count(UnitTypes.STARPORT_TECH_LAB) > 0)
                        {
                            if (Minerals() >= 50
                                && Gas() >= 25)
                                agent.Order(488);
                        }
                        else if (Minerals() >= 50
                            && Gas() >= 50)
                            agent.Order(487);
                    }
                    else if (Minerals() > 150
                        && Gas() >= 75
                        && FoodLeft() >= 2)
                        agent.Order(624);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_REACTOR)
                {
                    if (Count(UnitTypes.VIKING_FIGHTER) < DesiredVikings)
                    {
                        if (Minerals() > 150
                          && Gas() >= 75
                            && FoodLeft() >= 2)
                            agent.Order(624);
                    }
                    else if (Count(UnitTypes.MEDIVAC) < (SkyTossDetected ? 2 : 4))
                    {
                        if (Minerals() >= 100
                            && Gas() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(620);
                    }
                    else
                    {
                        if (Minerals() >= 150
                            && Gas() >= 150
                            && FoodLeft() >= 3
                            && !SkyTossDetected)
                            agent.Order(626);
                    }
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_TECH_LAB)
                {
                    if (Minerals() > 100
                        && Gas() >= 200
                        && Count(UnitTypes.RAVEN) < 2
                        && FoodLeft() >= 2)
                    {
                        agent.Order(622);
                    }
                    else if (Minerals() > 150
                        && Gas() >= 150
                        && FoodLeft() >= 3
                        && Count(UnitTypes.RAVEN) >= 2
                        && !SkyTossDetected)
                        agent.Order(626);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ARMORY)
            {
                if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(116)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(864);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(30)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(855);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(117)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(865);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(31)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(856);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(118)
                    && Gas() >= 250
                    && Minerals() >= 250)
                    agent.Order(866);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(32)
                    && Gas() >= 250
                    && Minerals() >= 250)
                    agent.Order(857);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
            {
                if (Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) > 0
                    && Gas() >= 150
                    && Minerals() >= 150
                    && FoodLeft() >= 2)
                    agent.Order(761);
                else if (Count(UnitTypes.CYCLONE) > 0
                    && Gas() >= 150
                    && Minerals() >= 150)
                    agent.Order(769);
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT_TECH_LAB
                    && Gas() >= 150
                    && Minerals() >= 150)
            {
                agent.Order(805);
            }
            else if (agent.Unit.UnitType == UnitTypes.ENGINEERING_BAY
                    && Gas() >= 100
                    && Minerals() >= 100
                    && Count(UnitTypes.PLANETARY_FORTRESS) > 0)
            {
                agent.Order(650);
            }
            else if (agent.Unit.UnitType == UnitTypes.BARRACKS_TECH_LAB)
            {
                if (Gas() >= 50
                    && Minerals() >= 50
                    && !Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(17))
                    agent.Order(732);
                else if (Gas() >= 100
                    && Minerals() >= 100
                    && Count(UnitTypes.MARINE) >= 5
                    && !Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(16))
                    agent.Order(731);
            }
        }
    }
}
