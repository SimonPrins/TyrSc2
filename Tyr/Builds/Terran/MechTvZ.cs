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
    public class MechTvZ : Build
    {
        private int MinVikings = 3;
        private int MaxVikings = 3;
        private bool DefendMutas = false;
        private bool ManyLings = false;
        private bool RoachHydra = false;

        private bool LingRush = false;

        private int DesiredTanks = 0;
        private int DesiredMines = 2;

        private int MinimumHellbats = 0;

        private bool InitialAttackDone = false;

        private DistributeOverBasesTask DistributeHellbatsTask;

        private List<CustomController> AttackMicroControllers = new List<CustomController>();

        private WallInCreator WallIn;
        private VikingController VikingController = new VikingController();

        private bool ScanTimingsSet = false;

        private bool UltralisksDetected = false;

        List<DefenseSquadTask> CycloneDefenseSquads;

        private int BlueFlameStarted = -1;

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            DefenseTask.Enable();
            BunkerDefendersTask.Enable();
            SupplyDepotTask.Enable();
            ArmyRavenTask.Enable();
            RepairTask.Enable();
            ReplenishBuildingSCVTask.Enable();
            DistributedDefenseTask.Enable();
            TransformTask.Enable();
            KillScoutsTask.Enable();
            HomeRepairTask.Enable();
            MechDestroyExpandsTask.Enable();

            DistributeHellbatsTask = new DistributeOverBasesTask(UnitTypes.HELLBAT);
            DistributeHellbatsTask.Enable();

            if (CycloneDefenseSquads == null)
                CycloneDefenseSquads = DefenseSquadTask.GetDefenseTasks(UnitTypes.CYCLONE);
            else
                foreach (DefenseSquadTask task in CycloneDefenseSquads)
                    Bot.Main.TaskManager.Add(task);
            DefenseSquadTask.Enable(CycloneDefenseSquads, true, true);

            foreach (DefenseSquadTask task in CycloneDefenseSquads)
            {
                task.Priority = 4;
                task.MaxDefenders = 1;
                task.AllowClaiming = false;
            }
        }

        public override string Name()
        {
            return "MechTvZ";
        }

        public override void OnStart(Bot tyr)
        {
            AttackMicroControllers.Add(new SoftLeashController(new HashSet<uint>() { UnitTypes.LIBERATOR, UnitTypes.MEDIVAC, UnitTypes.HELLBAT },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED, UnitTypes.THOR },
                12));
            AttackMicroControllers.Add(new HellionHarassController());

            AttackMicroControllers.Add(new SoftLeashController(new HashSet<uint>() { UnitTypes.CYCLONE },
                new HashSet<uint>() { UnitTypes.HELLBAT },
                12));
            AttackMicroControllers.Add(new LeashController(new HashSet<uint>() { UnitTypes.CYCLONE },
                new HashSet<uint>() { UnitTypes.HELLBAT },
                4));

            MicroControllers.Add(new TankController());
            MicroControllers.Add(new LiberatorController());
            MicroControllers.Add(VikingController);
            MicroControllers.Add(new MedivacController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new MineController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT });
                WallIn.ReserveSpace();
            }

            Set += SupplyDepots();
            Set += Turrets();
            Set += MainBuild();
        }

        public BuildList SupplyDepots()
        {
            BuildList result = new BuildList();

            result.If(() => { return Count(UnitTypes.SUPPLY_DEPOT) >= 3; });
            result.If(() =>
            {
                return Build.FoodUsed()
                    + Bot.Main.UnitManager.Count(UnitTypes.COMMAND_CENTER)
                    + Bot.Main.UnitManager.Count(UnitTypes.BARRACKS) * 2
                    + Bot.Main.UnitManager.Count(UnitTypes.FACTORY) * 2
                    + Bot.Main.UnitManager.Count(UnitTypes.STARPORT) * 2
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

            result.If(() => { return DefendMutas; });
            result.Building(UnitTypes.ENGINEERING_BAY);
            foreach (Base b in Bot.Main.BaseManager.Bases)
                result.Building(UnitTypes.MISSILE_TURRET, b, () => { return b.Owner == Bot.Main.PlayerId && b.ResourceCenter != null; });
            result.Building(UnitTypes.MISSILE_TURRET, Main);
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.FACTORY);
            result.If(() => !LingRush || Completed(UnitTypes.HELLION) + Completed(UnitTypes.HELLBAT) >= 4);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.REFINERY, () => InitialAttackDone);
            result.If(() => { return InitialAttackDone || Count(UnitTypes.HELLION) + Count(UnitTypes.HELLBAT) >= 8; });
            result.Building(UnitTypes.ARMORY);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.STARPORT);
            result.If(() => { return Count(UnitTypes.VIKING_FIGHTER) > 0; });
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.BUNKER, Natural, NaturalDefensePos);
            result.If(() => { return Count(UnitTypes.THOR) >= 1; });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.FACTORY, 2);
            result.Building(UnitTypes.FACTORY, () => { return Minerals() >= 600 && Gas() >= 400; });

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            WorkerScoutTask.Task.ScoutNatural = true;
            WorkerScoutTask.Task.StartFrame = 224;
            if (!InitialAttackDone 
                && TimingAttackTask.Task.AttackSent 
                && Completed(UnitTypes.HELLBAT) + Completed(UnitTypes.HELLION) + Completed(UnitTypes.MARINE) <= 4)
            {
                InitialAttackDone = true;
                for (int i = 0; i < AttackMicroControllers.Count; i++)
                {
                    if (AttackMicroControllers[i] is HellionHarassController)
                    {
                        AttackMicroControllers.RemoveAt(i);
                        break;
                    }
                }
            }
            if (!InitialAttackDone && (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) > 0
                || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK) >= 3
                || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.RAVAGER) > 0
                || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.BANELING) >= 2
                || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.QUEEN) >= 4)
                || LingRush)
            {
                InitialAttackDone = true;
                TimingAttackTask.Task.Clear();
                for (int i = 0; i < AttackMicroControllers.Count; i++)
                {
                    if (AttackMicroControllers[i] is HellionHarassController)
                    {
                        AttackMicroControllers.RemoveAt(i);
                        break;
                    }
                }
            }
            if (InitialAttackDone)
            {
                TimingAttackTask.Task.RequiredSize = 40;
                TimingAttackTask.Task.RetreatSize = 12;
            } else
            {
                TimingAttackTask.Task.RequiredSize = 8;
                TimingAttackTask.Task.RetreatSize = 3;
            }

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MUTALISK) > 0)
                DesiredTanks = 0;
            else if (DesiredTanks == 0)
            {
                foreach (Unit enemy in tyr.Enemies())
                    if (
                        (enemy.UnitType == UnitTypes.QUEEN || enemy.UnitType == UnitTypes.SPINE_CRAWLER || enemy.UnitType == UnitTypes.SPINE_CRAWLER_UPROOTED)
                        && SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 40 * 40)
                    {
                        DesiredTanks = 2;
                        break;
                    }
            }

            if (!LingRush && tyr.Frame <= 22.4 * 150
                && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 5)
                LingRush = true;

            TimingAttackTask.Task.BeforeControllers = AttackMicroControllers;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.Priority = 4;

            RepairTask.Task.WallIn = WallIn;
            
            SupplyDepotTask.Task.RaiseWall = LingRush && Completed(UnitTypes.HELLION) + Completed(UnitTypes.HELLBAT) < 4 ? WallIn : null;

            TransformTask.Task.HellionsToHellbats();

            MechDestroyExpandsTask.Task.Stopped = BlueFlameStarted < 0 || tyr.Frame < BlueFlameStarted + 22.4 * 20 || UltralisksDetected || DefendMutas || RoachHydra;

            if (!UltralisksDetected && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ULTRALISK) + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ULTRALISK_CAVERN) > 0)
                UltralisksDetected = true;

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MUTALISK) > 0)
            {
                MinVikings = 6;
                MaxVikings = 6;
                DefendMutas = true;
            }
            else if (UltralisksDetected)
            {
                MinVikings = 3;
                MaxVikings = 3;
            }
            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 15)
                ManyLings = true;
            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK) >= 10)
                RoachHydra = true;

            if (ManyLings
                && !DefendMutas
                && !RoachHydra)
                MinimumHellbats = 12;
            else if (!InitialAttackDone)
                MinimumHellbats = 8;
            else MinimumHellbats = 0;

            if (DefendMutas)
            {
                TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.VIKING_FIGHTER);
                DistributedDefenseTask.AirDefenseTask.AllowedDefenderTypes.Add(UnitTypes.VIKING_FIGHTER);
                VikingController.StickToTanks = false;
            }

            if (DefendMutas && !TimingAttackTask.Task.IsNeeded())
                DefenseSquadTask.Enable(CycloneDefenseSquads, true, true);
            else
                foreach (DefenseSquadTask task in CycloneDefenseSquads)
                {
                    task.Stopped = true;
                    task.Clear();
                }

            if (Count(UnitTypes.SCV) < 10 || (Gas() >= 800 && Minerals() < 500))
                GasWorkerTask.WorkersPerGas = 0;
            else if (Gas() >= 700 && Minerals() < 500)
                GasWorkerTask.WorkersPerGas = 1;
            else if (Gas() >= 600 && Minerals() < 500)
                GasWorkerTask.WorkersPerGas = 2;
            else
                GasWorkerTask.WorkersPerGas = 3;

            DistributeHellbatsTask.Stopped = Count(UnitTypes.COMMAND_CENTER) < 2
                || Completed(UnitTypes.HELLBAT) < 2 * (Count(UnitTypes.COMMAND_CENTER) - 1);

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANELING) > 0
                || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MUTALISK) > 0)
                DesiredMines = 6;

            if (tyr.TargetManager.PotentialEnemyStartLocations.Count == 1
                && !ScanTimingsSet)
            {
                ScanTimingsSet = true;
                tyr.OrbitalAbilityManager.SaveEnergy = 50;
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 8.5)
                });
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.MapAnalyzer.GetEnemyNatural().Pos,
                    FromFrame = (int)(22.4 * 60 * 8.5 + 22.4)
                });
            }

            if (BlueFlameStarted < 0 && tyr.UnitManager.ActiveOrders.Contains(761))
                BlueFlameStarted = tyr.Frame;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Completed(UnitTypes.BARRACKS) > 0
                && Count(UnitTypes.SCV) >= 16
                && (agent.Base == Main
                    || agent.Base == Natural))
            {
                if (Minerals() >= 150)
                    agent.Order(1516);
            }
            else if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Completed(UnitTypes.ENGINEERING_BAY) > 0
                && Count(UnitTypes.SCV) >= 16
                && Gas() >= 150
                && agent.Base != Main
                && agent.Base != Natural)
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
                if (Minerals() >= 50
                    && FoodLeft() >= 1
                    && Count(UnitTypes.MARINE) < 4
                    && (!Expanded.Get().Detected || Count(UnitTypes.BUNKER) > 0))
                    agent.Order(560);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if ((Count(UnitTypes.FACTORY_REACTOR) == 0 || Count(UnitTypes.FACTORY_TECH_LAB) >= 3)
                        && Count(UnitTypes.FACTORY_REACTOR) < 2)
                        agent.Order(455);
                    else
                    {
                        if (InitialAttackDone || Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) >= 8)
                            agent.Order(454);
                        else if (Minerals() >= 100
                            && FoodLeft() >= 2)
                        {
                            if (Completed(UnitTypes.ARMORY) > 0)
                                agent.Order(596);
                            else
                                agent.Order(595);
                        }
                    }
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                {
                    if (BlueFlameStarted < 0 
                        && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.QUEEN) >= 3
                        && Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) > 0)
                        return;
                    if (tyr.Frame <= 22.4 * 5
                        && !MechDestroyExpandsTask.Task.Stopped
                        && Minerals() >= 100
                        && FoodLeft() >= 2)
                    {
                        if (Completed(UnitTypes.ARMORY) > 0)
                            agent.Order(596);
                        else
                            agent.Order(595);

                    }
                    if (!DefendMutas
                        && RoachHydra
                        && Count(UnitTypes.THOR) >= Count(UnitTypes.SIEGE_TANK) * 3 + 3)
                    {
                        if (Minerals() >= 150
                            && Gas() >= 125
                            && FoodLeft() >= 3)
                            agent.Order(591);
                    }
                    else if (Count(UnitTypes.SIEGE_TANK) < DesiredTanks)
                    {
                        if (Minerals() >= 150
                            && Gas() >= 125
                            && FoodLeft() >= 3)
                            agent.Order(591);
                    }
                    else if (Minerals() >= 150
                        && Gas() >= 100
                        && FoodLeft() >= 3
                        && (Count(UnitTypes.CYCLONE) < 1 || DefendMutas || ManyLings)
                        && (Count(UnitTypes.CYCLONE) < 3 || DefendMutas)
                        && Count(UnitTypes.CYCLONE) < 5)
                        agent.Order(597);
                    else if (Completed(UnitTypes.ARMORY) > 0
                        && Minerals() >= 300
                        && Gas() >= 200
                        && FoodLeft() >= 6
                        && Count(UnitTypes.THOR) < 8)
                        agent.Order(594);
                    else if (Minerals() >= 100
                        && FoodLeft() >= 2
                        && Count(UnitTypes.THOR) >= 8)
                    {
                        if (Completed(UnitTypes.ARMORY) > 0)
                            agent.Order(596);
                        else
                            agent.Order(595);
                    }
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR)
                {
                    if (Count(UnitTypes.WIDOW_MINE) < DesiredMines
                        && InitialAttackDone
                        && Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) >= (!MechDestroyExpandsTask.Task.Stopped ? 12 : 4)
                        && (Count(UnitTypes.WIDOW_MINE) < 4 || Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) >= 10))
                    {
                        if (Minerals() >= 75
                            && Gas() >= 25
                            && FoodLeft() >= 2)
                                agent.Order(614);
                    } else if (!UltralisksDetected || Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) < 8)
                    {
                        if (Minerals() >= 100
                            && FoodLeft() >= 2
                            && (!MechDestroyExpandsTask.Task.Stopped || Gas() < 200 || Minerals() >= 400 || Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) < MinimumHellbats))
                        {
                            if (Completed(UnitTypes.ARMORY) > 0)
                                agent.Order(596);
                            else
                                agent.Order(595);
                        }
                    }
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.VIKING_FIGHTER) > 0)
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
                    if ((Count(UnitTypes.VIKING_FIGHTER) < MinVikings || Count(UnitTypes.MEDIVAC) >= (DefendMutas ? 2 : 4))
                        && Count(UnitTypes.VIKING_FIGHTER) < MaxVikings)
                    {
                        if (Minerals() > 150
                            && Gas() >= 75
                            && FoodLeft() >= 2)
                            agent.Order(624);
                    }
                    else if (Count(UnitTypes.MEDIVAC) < (DefendMutas ? 2 : 4)
                        && Count(UnitTypes.THOR) > 0
                        && !UltralisksDetected)
                    {
                        if (Minerals() >= 100
                            && Gas() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(620);
                    }
                    else if (Count(UnitTypes.THOR) > 0)
                    {
                        if (Minerals() >= 500
                            && Gas() >= 300
                            && FoodLeft() >= 3)
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
                        && Gas() >= 75
                        && FoodLeft() >= 2
                        && Count(UnitTypes.RAVEN) >= 2
                        && Count(UnitTypes.VIKING_FIGHTER) < MaxVikings)
                        agent.Order(624);
                    else if (Count(UnitTypes.THOR) > 0)
                    {
                        if (Minerals() >= 500
                            && Gas() >= 300
                            && FoodLeft() >= 3)
                            agent.Order(626);
                    }
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ARMORY && Count(UnitTypes.THOR) > 0)
            {
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(116)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(864);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(30)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(855);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(117)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(865);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(31)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(856);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(118)
                    && Gas() >= 250
                    && Minerals() >= 250)
                    agent.Order(866);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(32)
                    && Gas() >= 250
                    && Minerals() >= 250)
                    agent.Order(857);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
            {
                if (Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) > 0
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(19))
                {
                    if (Gas() >= 150
                        && Minerals() >= 150)
                        agent.Order(761);
                }
                else if (Count(UnitTypes.CYCLONE) > 0
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(144))
                {
                    if (Gas() >= 150
                        && Minerals() >= 150)
                        agent.Order(769);
                }
                else if (Count(UnitTypes.WIDOW_MINE) > 0
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(122))
                {
                    if (Gas() >= 75
                        && Minerals() >= 75)
                        agent.Order(764);
                }
            }
        }
    }
}
